namespace StellarImperiums.Application.Users.Queries;

/// <summary>
/// Query issued to load the profile of the authenticated user.
/// </summary>
/// <param name="UserId">The user identifier extracted from the access token subject claim.</param>
public sealed record GetCurrentUserQuery(int UserId);
