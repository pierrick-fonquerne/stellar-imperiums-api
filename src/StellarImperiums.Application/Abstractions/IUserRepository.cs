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
    /// Persists pending changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
