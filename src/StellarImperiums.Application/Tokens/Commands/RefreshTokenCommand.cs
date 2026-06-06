namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Command issued to exchange a refresh token for a new access token and a rotated refresh token.
/// </summary>
/// <param name="RefreshTokenValue">The opaque refresh token value read from the client cookie.</param>
public sealed record RefreshTokenCommand(string RefreshTokenValue);
