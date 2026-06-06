using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Tokens.Exceptions;

namespace StellarImperiums.Domain.Tokens;

/// <summary>
/// Entity representing one link of a refresh token rotation chain for a user session.
/// </summary>
/// <remarks>
/// Only the SHA-256 hash of the opaque token value is stored. Tokens issued by successive
/// rotations share the same <see cref="FamilyId"/>, which allows revoking the whole chain
/// when a rotated token is replayed. The parameterless constructor exists solely for
/// Entity Framework Core materialization.
/// </remarks>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// Gets the identifier of the user owning this token.
    /// </summary>
    public int UserId { get; private set; }

    /// <summary>
    /// Gets the SHA-256 hash (uppercase hexadecimal) of the opaque token value.
    /// </summary>
    public string TokenHash { get; private set; }

    /// <summary>
    /// Gets the identifier shared by every token of the same rotation chain.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>
    /// Gets the creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the expiry timestamp (UTC).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the revocation timestamp (UTC), or <c>null</c> if the token was never revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Gets the hash of the successor token, or <c>null</c> if this token was never rotated.
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the token has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Gets a value indicating whether the token has been rotated to a successor.
    /// </summary>
    public bool IsRotated => ReplacedByTokenHash is not null;

    private RefreshToken()
    {
        TokenHash = null!;
    }

    /// <summary>
    /// Creates a new <see cref="RefreshToken"/> in the active state.
    /// </summary>
    /// <param name="userId">The identifier of the owning user.</param>
    /// <param name="tokenHash">The SHA-256 hash of the opaque token value.</param>
    /// <param name="familyId">The rotation chain identifier.</param>
    /// <param name="expiresAt">The expiry timestamp (must be after <paramref name="createdAt"/>).</param>
    /// <param name="createdAt">The creation timestamp (defaults to the current UTC time).</param>
    /// <returns>The newly created token.</returns>
    /// <exception cref="InvalidRefreshTokenException">
    /// Thrown when the hash is empty, the family identifier is empty, or the expiry is not after the creation time.
    /// </exception>
    public static RefreshToken Create(
        int userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt,
        DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new InvalidRefreshTokenException("Token hash cannot be empty.");
        }

        if (familyId == Guid.Empty)
        {
            throw new InvalidRefreshTokenException("Family id cannot be empty.");
        }

        var creation = createdAt ?? DateTimeOffset.UtcNow;
        if (expiresAt <= creation)
        {
            throw new InvalidRefreshTokenException("Expiry must be after the creation time.");
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAt = creation,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Indicates whether the token is expired at the given instant.
    /// </summary>
    /// <param name="now">The instant to evaluate against (UTC).</param>
    /// <returns><c>true</c> when the token is expired.</returns>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>
    /// Indicates whether the token can still be used at the given instant.
    /// </summary>
    /// <param name="now">The instant to evaluate against (UTC).</param>
    /// <returns><c>true</c> when the token is neither revoked, rotated, nor expired.</returns>
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsRotated && !IsExpired(now);

    /// <summary>
    /// Rotates the token: marks it as replaced and returns its successor in the same family.
    /// </summary>
    /// <param name="newTokenHash">The hash of the successor token.</param>
    /// <param name="newExpiresAt">The expiry of the successor token.</param>
    /// <param name="now">The rotation instant (UTC).</param>
    /// <returns>The successor token.</returns>
    /// <exception cref="RefreshTokenRevokedException">Thrown when the token is revoked.</exception>
    /// <exception cref="RefreshTokenReuseException">Thrown when the token was already rotated.</exception>
    /// <exception cref="RefreshTokenExpiredException">Thrown when the token is expired.</exception>
    /// <exception cref="InvalidRefreshTokenException">Thrown when the successor hash or expiry is invalid.</exception>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset newExpiresAt, DateTimeOffset now)
    {
        if (IsRevoked)
        {
            throw new RefreshTokenRevokedException();
        }

        if (IsRotated)
        {
            throw new RefreshTokenReuseException();
        }

        if (IsExpired(now))
        {
            throw new RefreshTokenExpiredException();
        }

        var successor = Create(UserId, newTokenHash, FamilyId, newExpiresAt, now);
        ReplacedByTokenHash = newTokenHash;
        return successor;
    }

    /// <summary>
    /// Revokes the token. Calling this method on an already revoked token keeps the original timestamp.
    /// </summary>
    /// <param name="when">The revocation instant (UTC).</param>
    public void Revoke(DateTimeOffset when)
    {
        if (RevokedAt is null)
        {
            RevokedAt = when;
        }
    }
}
