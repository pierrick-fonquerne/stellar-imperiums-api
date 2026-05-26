using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Users.Exceptions;

/// <summary>
/// Thrown when an attempt is made to suspend a user that is already suspended.
/// </summary>
public sealed class UserAlreadySuspendedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserAlreadySuspendedException"/> class.
    /// </summary>
    public UserAlreadySuspendedException()
        : base("The user is already suspended.")
    {
    }
}
