using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StellarImperiums.Api.Contracts.Users;
using StellarImperiums.Application.Users.Queries;
using Wolverine;

namespace StellarImperiums.Api.Controllers.V1;

/// <summary>
/// Endpoints exposing user profiles.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UsersController(IMessageBus messageBus) : ControllerBase
{
    /// <summary>
    /// Returns the profile of the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The authenticated user's profile.</returns>
    /// <response code="200">The profile of the authenticated user.</response>
    /// <response code="401">The access token is missing, invalid, or its subject no longer exists.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> GetMe(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue("sub");
        if (!int.TryParse(subject, CultureInfo.InvariantCulture, out var userId))
        {
            return Unauthorized();
        }

        var result = await messageBus
            .InvokeAsync<CurrentUserResult>(new GetCurrentUserQuery(userId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(new MeResponse(
            result.Id,
            result.Username,
            result.Email,
            result.Role,
            result.RegistrationDate,
            result.LastLoginAt));
    }
}
