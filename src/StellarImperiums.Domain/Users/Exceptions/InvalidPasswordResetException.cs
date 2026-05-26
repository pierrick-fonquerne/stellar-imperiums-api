using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Users.Exceptions;

/// <summary>
/// Thrown when a password reset request is rejected (empty token, expiry in the past, etc.).
/// </summary>
public sealed class InvalidPasswordResetException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordResetException"/> class.
    /// </summary>
    /// <param name="message">The reason the reset request was rejected.</param>
    public InvalidPasswordResetException(string message)
        : base(message)
    {
    }
}
