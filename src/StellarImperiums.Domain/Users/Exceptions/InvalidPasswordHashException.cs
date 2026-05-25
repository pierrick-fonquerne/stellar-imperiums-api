using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Users.Exceptions;

/// <summary>
/// Thrown when a password hash is not in the expected PHC argon2id format or exceeds the maximum length.
/// </summary>
public sealed class InvalidPasswordHashException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordHashException"/> class.
    /// </summary>
    /// <param name="message">The reason the hash was rejected.</param>
    public InvalidPasswordHashException(string message)
        : base(message)
    {
    }
}
