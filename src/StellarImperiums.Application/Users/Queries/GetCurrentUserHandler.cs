using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;

namespace StellarImperiums.Application.Users.Queries;

/// <summary>
/// Wolverine handler that loads the authenticated user's profile.
/// </summary>
public sealed class GetCurrentUserHandler(IUserRepository users)
{
    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="query">The query carrying the authenticated user identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The user's profile.</returns>
    /// <exception cref="UserNotFoundException">Thrown when the user no longer exists.</exception>
    public async Task<CurrentUserResult> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new UserNotFoundException();

        return new CurrentUserResult(
            user.Id,
            user.Username.Value,
            user.Email.Value,
            user.Role.ToString(),
            user.RegistrationDate,
            user.LastLoginAt);
    }
}
