using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Utilisateurs.Exceptions;

/// <summary>
/// Thrown when a password hash is not in the expected PHC argon2id format or exceeds the maximum length.
/// </summary>
public sealed class MotDePasseHashInvalideException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MotDePasseHashInvalideException"/> class.
    /// </summary>
    /// <param name="message">The reason the hash was rejected.</param>
    public MotDePasseHashInvalideException(string message)
        : base(message)
    {
    }
}
