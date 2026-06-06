namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Command issued to terminate a session by revoking its refresh token family.
/// </summary>
/// <param name="RefreshTokenValue">
/// The opaque refresh token value read from the client cookie, or <c>null</c> when no cookie was sent.
/// </param>
public sealed record LogoutCommand(string? RefreshTokenValue);
