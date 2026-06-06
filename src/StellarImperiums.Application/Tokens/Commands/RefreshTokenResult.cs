namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Result returned after a successful refresh token rotation.
/// </summary>
/// <param name="AccessToken">The new serialized signed access token.</param>
/// <param name="ExpiresInSeconds">The access token lifetime in seconds.</param>
/// <param name="RefreshTokenValue">The new opaque refresh token value to set as a cookie.</param>
/// <param name="RefreshTokenExpiresAt">The new refresh token expiry used as the cookie lifetime.</param>
public sealed record RefreshTokenResult(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshTokenValue,
    DateTimeOffset RefreshTokenExpiresAt);
