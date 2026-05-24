using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Utilisateurs.Exceptions;

/// <summary>
/// Thrown when a pseudo does not satisfy the domain rules (empty, too short, too long).
/// </summary>
public sealed class PseudoInvalideException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PseudoInvalideException"/> class.
    /// </summary>
    /// <param name="message">The reason the pseudo was rejected.</param>
    public PseudoInvalideException(string message)
        : base(message)
    {
    }
}
