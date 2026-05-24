using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Utilisateurs.Exceptions;

/// <summary>
/// Thrown when an email address does not satisfy the domain rules (empty, too long, malformed).
/// </summary>
public sealed class EmailInvalideException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailInvalideException"/> class.
    /// </summary>
    /// <param name="message">The reason the email was rejected.</param>
    public EmailInvalideException(string message)
        : base(message)
    {
    }
}
