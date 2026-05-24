using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Utilisateurs.Exceptions;

/// <summary>
/// Thrown when an attempt is made to suspend a user that is already suspended.
/// </summary>
public sealed class UtilisateurDejaSuspenduException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UtilisateurDejaSuspenduException"/> class.
    /// </summary>
    public UtilisateurDejaSuspenduException()
        : base("The user is already suspended.")
    {
    }
}
