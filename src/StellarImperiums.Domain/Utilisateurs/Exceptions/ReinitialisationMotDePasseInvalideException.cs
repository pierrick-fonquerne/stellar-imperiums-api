using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Utilisateurs.Exceptions;

/// <summary>
/// Thrown when a password reset request is rejected (empty token, expiry in the past, etc.).
/// </summary>
public sealed class ReinitialisationMotDePasseInvalideException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReinitialisationMotDePasseInvalideException"/> class.
    /// </summary>
    /// <param name="message">The reason the reset request was rejected.</param>
    public ReinitialisationMotDePasseInvalideException(string message)
        : base(message)
    {
    }
}
