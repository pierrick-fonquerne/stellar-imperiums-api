namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a registration attempt collides with an existing email.
/// </summary>
public sealed class EmailAlreadyTakenException : Exception
{
    /// <summary>
    /// Gets the email that triggered the collision.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAlreadyTakenException"/> class.
    /// </summary>
    /// <param name="email">The email that is already taken.</param>
    public EmailAlreadyTakenException(string email)
        : base($"The email '{email}' is already in use.")
    {
        Email = email;
    }
}
