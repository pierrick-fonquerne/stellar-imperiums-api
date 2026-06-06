using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on a revoked refresh token.
/// </summary>
public sealed class RefreshTokenRevokedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRevokedException"/> class.
    /// </summary>
    public RefreshTokenRevokedException()
        : base("The refresh token has been revoked.")
    {
    }
}
