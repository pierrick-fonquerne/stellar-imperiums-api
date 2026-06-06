namespace StellarImperiums.Application.Tokens.Exceptions;

/// <summary>
/// Thrown when a presented refresh token is unknown, expired, revoked, or replayed.
/// The message is intentionally generic so the API never reveals which check failed.
/// </summary>
public sealed class RefreshTokenRejectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRejectedException"/> class.
    /// </summary>
    public RefreshTokenRejectedException()
        : base("Invalid or expired refresh token.")
    {
    }
}
