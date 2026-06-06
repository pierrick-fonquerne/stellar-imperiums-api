using Microsoft.EntityFrameworkCore;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUserRepository"/>.
/// </summary>
/// <remarks>
/// Existence checks use the <c>AsNoTracking</c> pattern to avoid pulling entities into the change tracker.
/// </remarks>
public sealed class EfUserRepository(StellarDbContext dbContext) : IUserRepository
{
    /// <inheritdoc />
    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task<bool> UsernameExistsAsync(Username username, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
