using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="User"/> aggregate root.
/// </summary>
/// <remarks>
/// The repository exposes only business-meaningful operations. Persistence side effects are
/// committed by calling <see cref="SaveChangesAsync"/>, which delegates to the underlying unit of work.
/// </remarks>
public interface IUserRepository
{
    /// <summary>
    /// Adds a new <see cref="User"/> to the persistence context (no commit).
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether a user with the given username already exists.
    /// </summary>
    /// <param name="username">The username to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<bool> UsernameExistsAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether a user with the given email already exists.
    /// </summary>
    /// <param name="email">The email to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by email address.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked user, or <c>null</c> when no user matches.</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked user, or <c>null</c> when no user matches.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by the hash of their pending password reset token.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash of the reset token to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked user, or <c>null</c> when no user matches.</returns>
    Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
