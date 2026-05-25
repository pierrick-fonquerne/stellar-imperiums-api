using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Users.Exceptions;

/// <summary>
/// Thrown when an attempt is made to reactivate a user that is not currently suspended.
/// </summary>
public sealed class UserNotSuspendedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotSuspendedException"/> class.
    /// </summary>
    public UserNotSuspendedException()
        : base("The user is not currently suspended.")
    {
    }
}
