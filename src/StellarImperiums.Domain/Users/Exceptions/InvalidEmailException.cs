using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Users.Exceptions;

/// <summary>
/// Thrown when an email address does not satisfy the domain rules (empty, too long, malformed).
/// </summary>
public sealed class InvalidEmailException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEmailException"/> class.
    /// </summary>
    /// <param name="message">The reason the email was rejected.</param>
    public InvalidEmailException(string message)
        : base(message)
    {
    }
}
