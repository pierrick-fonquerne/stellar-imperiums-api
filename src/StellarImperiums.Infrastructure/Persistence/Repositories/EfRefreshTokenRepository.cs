using Microsoft.EntityFrameworkCore;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Domain.Tokens;

namespace StellarImperiums.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IRefreshTokenRepository"/>.
/// </summary>
/// <remarks>
/// Family revocation loads the affected tokens and applies the domain transition on each one,
/// so the revocation rules stay inside the entity instead of leaking into a bulk SQL update.
/// </remarks>
public sealed class EfRefreshTokenRepository(StellarDbContext dbContext) : IRefreshTokenRepository
{
    /// <inheritdoc />
    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        return dbContext.RefreshTokens.AddAsync(token, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenHash);
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset when, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.Revoke(when);
        }
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(int userId, DateTimeOffset when, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.Revoke(when);
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
