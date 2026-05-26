namespace StellarImperiums.Domain.Users;

/// <summary>
/// Represents the functional role assigned to a user.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Standard player account with access to gameplay features only.
    /// </summary>
    Player = 0,

    /// <summary>
    /// Administrator account with elevated permissions for backoffice operations.
    /// </summary>
    Admin = 1
}
