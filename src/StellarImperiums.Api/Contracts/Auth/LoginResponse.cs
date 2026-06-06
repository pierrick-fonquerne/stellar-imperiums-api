namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// Response of the login endpoint. The refresh token is delivered separately as an HttpOnly cookie.
/// </summary>
/// <param name="AccessToken">The serialized signed access token.</param>
/// <param name="ExpiresIn">The access token lifetime in seconds.</param>
/// <param name="TokenType">The token type to use in the Authorization header (always <c>Bearer</c>).</param>
/// <param name="MustChangePassword">Whether the user must change their password before playing.</param>
/// <param name="User">A summary of the authenticated user.</param>
public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType,
    bool MustChangePassword,
    UserSummary User);

/// <summary>
/// Summary of the authenticated user returned at login.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role name.</param>
public sealed record UserSummary(int Id, string Username, string Email, string Role);
