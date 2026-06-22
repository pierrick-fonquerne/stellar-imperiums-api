namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a password reset token is not found, has already been used, or has expired.
/// </summary>
public sealed class InvalidPasswordResetTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordResetTokenException"/> class.
    /// </summary>
    public InvalidPasswordResetTokenException()
        : base("The password reset token is invalid or has expired.")
    {
    }
}
