using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Issues access tokens and opaque refresh token material for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed access token carrying the user's identity claims.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The serialized token and its lifetime in seconds.</returns>
    AccessTokenResult CreateAccessToken(User user);

    /// <summary>
    /// Generates a new cryptographically random refresh token.
    /// </summary>
    /// <returns>The opaque value to hand to the client, its hash to persist, and its expiry.</returns>
    RefreshTokenMaterial GenerateRefreshToken();

    /// <summary>
    /// Computes the persistence hash of an opaque refresh token value received from a client.
    /// </summary>
    /// <param name="refreshTokenValue">The opaque refresh token value.</param>
    /// <returns>The SHA-256 hash in uppercase hexadecimal.</returns>
    string HashRefreshToken(string refreshTokenValue);
}

/// <summary>
/// Result of an access token creation.
/// </summary>
/// <param name="Token">The serialized signed token.</param>
/// <param name="ExpiresInSeconds">The token lifetime in seconds.</param>
public sealed record AccessTokenResult(string Token, int ExpiresInSeconds);

/// <summary>
/// Material produced when generating a refresh token.
/// </summary>
/// <param name="Value">The opaque value handed to the client (never persisted).</param>
/// <param name="Hash">The SHA-256 hash persisted in place of the value.</param>
/// <param name="ExpiresAt">The expiry timestamp (UTC).</param>
public sealed record RefreshTokenMaterial(string Value, string Hash, DateTimeOffset ExpiresAt);
