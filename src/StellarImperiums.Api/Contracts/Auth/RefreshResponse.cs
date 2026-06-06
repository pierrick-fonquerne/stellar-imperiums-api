namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// Response of the refresh endpoint. The rotated refresh token is delivered as an HttpOnly cookie.
/// </summary>
/// <param name="AccessToken">The new serialized signed access token.</param>
/// <param name="ExpiresIn">The access token lifetime in seconds.</param>
/// <param name="TokenType">The token type to use in the Authorization header (always <c>Bearer</c>).</param>
public sealed record RefreshResponse(string AccessToken, int ExpiresIn, string TokenType);
