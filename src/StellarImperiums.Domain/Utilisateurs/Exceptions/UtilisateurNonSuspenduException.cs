using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Utilisateurs.Exceptions;

/// <summary>
/// Thrown when an attempt is made to reactivate a user that is not currently suspended.
/// </summary>
public sealed class UtilisateurNonSuspenduException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UtilisateurNonSuspenduException"/> class.
    /// </summary>
    public UtilisateurNonSuspenduException()
        : base("The user is not currently suspended.")
    {
    }
}
