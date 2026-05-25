using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Users.Exceptions;

/// <summary>
/// Thrown when a username does not satisfy the domain rules (empty, too short, too long).
/// </summary>
public sealed class InvalidUsernameException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidUsernameException"/> class.
    /// </summary>
    /// <param name="message">The reason the username was rejected.</param>
    public InvalidUsernameException(string message)
        : base(message)
    {
    }
}
