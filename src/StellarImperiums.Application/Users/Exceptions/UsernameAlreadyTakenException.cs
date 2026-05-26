namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a registration attempt collides with an existing username.
/// </summary>
public sealed class UsernameAlreadyTakenException : Exception
{
    /// <summary>
    /// Gets the username that triggered the collision.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UsernameAlreadyTakenException"/> class.
    /// </summary>
    /// <param name="username">The username that is already taken.</param>
    public UsernameAlreadyTakenException(string username)
        : base($"The username '{username}' is already in use.")
    {
        Username = username;
    }
}
