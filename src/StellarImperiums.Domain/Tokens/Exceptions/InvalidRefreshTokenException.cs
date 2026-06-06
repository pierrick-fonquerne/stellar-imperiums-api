using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when a refresh token cannot be created because an argument violates a domain invariant.
/// </summary>
public sealed class InvalidRefreshTokenException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRefreshTokenException"/> class.
    /// </summary>
    /// <param name="message">The reason the refresh token was rejected.</param>
    public InvalidRefreshTokenException(string message)
        : base(message)
    {
    }
}
