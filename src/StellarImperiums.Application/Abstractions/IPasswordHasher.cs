using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Abstraction over the password hashing algorithm used by the application.
/// </summary>
/// <remarks>
/// The plaintext password never leaves the application boundary: callers pass the user-provided
/// secret to <see cref="Hash"/> and persist only the returned <see cref="PasswordHash"/>.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the plaintext password and returns a <see cref="PasswordHash"/> in PHC argon2id format.
    /// </summary>
    /// <param name="plaintextPassword">The user-provided plaintext password.</param>
    /// <returns>A <see cref="PasswordHash"/> ready to be persisted.</returns>
    PasswordHash Hash(string plaintextPassword);

    /// <summary>
    /// Verifies that the plaintext password matches the stored hash.
    /// </summary>
    /// <param name="plaintextPassword">The plaintext password submitted by the user.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <returns><c>true</c> when the password matches, <c>false</c> otherwise.</returns>
    bool Verify(string plaintextPassword, PasswordHash hash);
}
