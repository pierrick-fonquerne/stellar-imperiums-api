using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when a refresh token that was already rotated is presented again, which signals a possible theft.
/// </summary>
public sealed class RefreshTokenReuseException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenReuseException"/> class.
    /// </summary>
    public RefreshTokenReuseException()
        : base("The refresh token has already been rotated.")
    {
    }
}
