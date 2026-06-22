using StellarImperiums.Domain.Tokens;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="RefreshToken"/> entity.
/// </summary>
/// <remarks>
/// Lookups track the returned entity so that domain state transitions (rotation, revocation)
/// are persisted by <see cref="SaveChangesAsync"/>.
/// </remarks>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Adds a new <see cref="RefreshToken"/> to the persistence context (no commit).
    /// </summary>
    /// <param name="token">The token to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a refresh token by the hash of its opaque value.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked token, or <c>null</c> when no token matches.</returns>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every non-revoked token of the given rotation family (no commit).
    /// </summary>
    /// <param name="familyId">The rotation chain identifier.</param>
    /// <param name="when">The revocation instant (UTC).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset when, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all non-revoked refresh tokens belonging to the given user (no commit).
    /// </summary>
    /// <param name="userId">The identifier of the user whose tokens must be revoked.</param>
    /// <param name="when">The revocation instant (UTC).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RevokeAllForUserAsync(int userId, DateTimeOffset when, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
