using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on a refresh token past its expiry.
/// </summary>
public sealed class RefreshTokenExpiredException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenExpiredException"/> class.
    /// </summary>
    public RefreshTokenExpiredException()
        : base("The refresh token has expired.")
    {
    }
}
