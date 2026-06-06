using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StellarImperiums.Api.Contracts.Auth;
using StellarImperiums.Application.Tokens.Commands;
using StellarImperiums.Application.Users.Commands;
using Wolverine;

namespace StellarImperiums.Api.Controllers.V1;

/// <summary>
/// Authentication endpoints (registration, login, password reset).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IMessageBus messageBus) : ControllerBase
{
    /// <summary>
    /// Registers a new player account.
    /// </summary>
    /// <param name="request">The registration payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The newly created user.</returns>
    /// <response code="201">The account was successfully created.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="409">The username or email is already in use.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Username, request.Email, request.Password);
        var result = await messageBus
            .InvokeAsync<RegisterUserResponse>(command, cancellationToken)
            .ConfigureAwait(false);

        var response = new RegisterResponse(
            result.Id,
            result.Username,
            result.Email,
            result.RegistrationDate);

        return CreatedAtAction(nameof(Register), new { id = response.Id }, response);
    }

    private const string RefreshTokenCookieName = "refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/auth";
    private const string BearerTokenType = "Bearer";

    /// <summary>
    /// Authenticates a player with email and password.
    /// </summary>
    /// <param name="request">The login payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token and the user summary; the refresh token is set as an HttpOnly cookie.</returns>
    /// <response code="200">The credentials are valid.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="401">The email or password is wrong.</response>
    /// <response code="403">The account is suspended.</response>
    /// <response code="429">Too many login attempts from this address.</response>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);
        var result = await messageBus
            .InvokeAsync<LoginUserResult>(command, cancellationToken)
            .ConfigureAwait(false);

        SetRefreshTokenCookie(result.RefreshTokenValue, result.RefreshTokenExpiresAt);

        var response = new LoginResponse(
            result.AccessToken,
            result.ExpiresInSeconds,
            BearerTokenType,
            result.MustChangePassword,
            new UserSummary(result.UserId, result.Username, result.Email, result.Role));

        return Ok(response);
    }

    /// <summary>
    /// Exchanges the refresh token cookie for a new access token and a rotated refresh token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The new access token; the rotated refresh token replaces the cookie.</returns>
    /// <response code="200">The refresh token was valid and has been rotated.</response>
    /// <response code="401">The refresh token is missing, unknown, expired, revoked, or replayed.</response>
    /// <response code="403">The account is suspended.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RefreshResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshTokenValue)
            || string.IsNullOrEmpty(refreshTokenValue))
        {
            return Problem(
                title: "Invalid refresh token",
                detail: "No refresh token cookie was provided.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await messageBus
            .InvokeAsync<RefreshTokenResult>(new RefreshTokenCommand(refreshTokenValue), cancellationToken)
            .ConfigureAwait(false);

        SetRefreshTokenCookie(result.RefreshTokenValue, result.RefreshTokenExpiresAt);

        return Ok(new RefreshResponse(result.AccessToken, result.ExpiresInSeconds, BearerTokenType));
    }

    /// <summary>
    /// Terminates the session by revoking the refresh token family and clearing the cookie.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>No content; logout is idempotent.</returns>
    /// <response code="204">The session is terminated (also returned when no cookie was sent).</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshTokenValue)
            && !string.IsNullOrEmpty(refreshTokenValue))
        {
            await messageBus
                .InvokeAsync(new LogoutCommand(refreshTokenValue), cancellationToken)
                .ConfigureAwait(false);
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string value, DateTimeOffset expiresAt) =>
        Response.Cookies.Append(RefreshTokenCookieName, value, BuildRefreshTokenCookieOptions(expiresAt));

    private void DeleteRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookieName, BuildRefreshTokenCookieOptions(null));

    private static CookieOptions BuildRefreshTokenCookieOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = RefreshTokenCookiePath,
        Expires = expiresAt
    };
}
