namespace StellarImperiums.Domain.Utilisateurs;

/// <summary>
/// Represents the functional role assigned to a user.
/// </summary>
public enum RoleUtilisateur
{
    /// <summary>
    /// Standard player account with access to gameplay features only.
    /// </summary>
    Joueur = 0,

    /// <summary>
    /// Administrator account with elevated permissions for backoffice operations.
    /// </summary>
    Admin = 1
}
