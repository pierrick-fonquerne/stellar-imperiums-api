namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a suspended user attempts to authenticate or refresh a session.
/// </summary>
public sealed class UserSuspendedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSuspendedException"/> class.
    /// </summary>
    public UserSuspendedException()
        : base("This account is suspended.")
    {
    }
}
