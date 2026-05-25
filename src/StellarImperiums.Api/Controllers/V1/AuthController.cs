using Microsoft.AspNetCore.Mvc;
using StellarImperiums.Api.Contracts.Auth;
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
}
