using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Common;

namespace StellarImperiums.Api.Middleware;

/// <summary>
/// Translates domain and application exceptions into RFC 7807 <see cref="ProblemDetails"/> HTTP responses.
/// </summary>
public sealed class DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
{
    /// <summary>
    /// Invokes the next middleware and converts known business exceptions into structured HTTP responses.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            logger.LogInformation(ex, "Validation failure for {Path}.", context.Request.Path);
            await WriteValidationProblemAsync(context, ex).ConfigureAwait(false);
        }
        catch (UsernameAlreadyTakenException ex)
        {
            logger.LogInformation("Username collision: {Username}.", ex.Username);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Username already taken", ex.Message).ConfigureAwait(false);
        }
        catch (EmailAlreadyTakenException ex)
        {
            logger.LogInformation("Email collision: {Email}.", ex.Email);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Email already taken", ex.Message).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Domain invariant violated for {Path}.", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Domain rule violation", ex.Message).ConfigureAwait(false);
        }
    }

    private static Task WriteValidationProblemAsync(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(problem);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://datatracker.ietf.org/doc/html/rfc9110#section-15.{(statusCode / 100)}.{statusCode % 100}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(problem);
    }
}
