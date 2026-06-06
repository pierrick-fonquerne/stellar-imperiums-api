namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Result returned after a successful login.
/// </summary>
/// <param name="AccessToken">The serialized signed access token.</param>
/// <param name="ExpiresInSeconds">The access token lifetime in seconds.</param>
/// <param name="UserId">The authenticated user identifier.</param>
/// <param name="Username">The authenticated user's username.</param>
/// <param name="Email">The authenticated user's email address.</param>
/// <param name="Role">The authenticated user's role name.</param>
/// <param name="MustChangePassword">Whether the user must change their password before playing.</param>
/// <param name="RefreshTokenValue">The opaque refresh token value to set as a cookie.</param>
/// <param name="RefreshTokenExpiresAt">The refresh token expiry used as the cookie lifetime.</param>
public sealed record LoginUserResult(
    string AccessToken,
    int ExpiresInSeconds,
    int UserId,
    string Username,
    string Email,
    string Role,
    bool MustChangePassword,
    string RefreshTokenValue,
    DateTimeOffset RefreshTokenExpiresAt);
