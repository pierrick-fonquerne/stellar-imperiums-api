namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a login attempt fails, without revealing whether the email or the password was wrong.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
