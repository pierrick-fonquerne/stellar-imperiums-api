namespace StellarImperiums.Application.Users.Queries;

/// <summary>
/// Profile of the authenticated user.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role name.</param>
/// <param name="RegistrationDate">The registration timestamp (UTC).</param>
/// <param name="LastLoginAt">The last successful login timestamp (UTC), or <c>null</c>.</param>
public sealed record CurrentUserResult(
    int Id,
    string Username,
    string Email,
    string Role,
    DateTimeOffset RegistrationDate,
    DateTimeOffset? LastLoginAt);
