namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when the user referenced by a valid credential no longer exists.
/// </summary>
public sealed class UserNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    public UserNotFoundException()
        : base("User not found.")
    {
    }
}
