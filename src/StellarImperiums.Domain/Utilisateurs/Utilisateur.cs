using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Utilisateurs;

/// <summary>
/// Root entity representing a registered user (player or administrator).
/// </summary>
/// <remarks>
/// State transitions (suspension, password change, password reset) are encapsulated in dedicated methods
/// that enforce the domain invariants. The parameterless constructor exists solely for Entity Framework
/// Core materialization and should not be used by application code.
/// </remarks>
public sealed class Utilisateur : Entity
{
    /// <summary>
    /// Gets the unique pseudo (display name) of the user.
    /// </summary>
    public Pseudo Pseudo { get; private set; }

    /// <summary>
    /// Gets the unique email address of the user.
    /// </summary>
    public Email Mail { get; private set; }

    /// <summary>
    /// Gets the current password hash (PHC argon2id format).
    /// </summary>
    public MotDePasseHash MotDePasse { get; private set; }

    /// <summary>
    /// Gets the functional role assigned to the user.
    /// </summary>
    public RoleUtilisateur Role { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user is currently suspended.
    /// </summary>
    public bool Suspendu { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user must change their password on next login.
    /// </summary>
    public bool NecessaireAModifier { get; private set; }

    /// <summary>
    /// Gets the registration timestamp (UTC).
    /// </summary>
    public DateTimeOffset DateInscription { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last successful login, or <c>null</c> if the user never logged in.
    /// </summary>
    public DateTimeOffset? DerniereConnexion { get; private set; }

    /// <summary>
    /// Gets the current password reset token, or <c>null</c> if no reset is in progress.
    /// </summary>
    public string? ResetToken { get; private set; }

    /// <summary>
    /// Gets the expiry timestamp of the current password reset token (UTC), or <c>null</c> if no reset is in progress.
    /// </summary>
    public DateTimeOffset? ResetTokenExpireLe { get; private set; }

    private Utilisateur()
    {
        Pseudo = null!;
        Mail = null!;
        MotDePasse = null!;
    }

    /// <summary>
    /// Creates a new <see cref="Utilisateur"/> in the active state.
    /// </summary>
    /// <param name="pseudo">The user's pseudo.</param>
    /// <param name="mail">The user's email address.</param>
    /// <param name="motDePasse">The user's password hash.</param>
    /// <param name="role">The functional role (defaults to <see cref="RoleUtilisateur.Joueur"/>).</param>
    /// <param name="necessaireAModifier">Whether the user must change their password on first login.</param>
    /// <param name="dateInscription">An explicit registration timestamp, useful for tests or seeding (defaults to <see cref="DateTimeOffset.UtcNow"/>).</param>
    /// <returns>A new <see cref="Utilisateur"/> ready to be persisted.</returns>
    public static Utilisateur Creer(
        Pseudo pseudo,
        Email mail,
        MotDePasseHash motDePasse,
        RoleUtilisateur role = RoleUtilisateur.Joueur,
        bool necessaireAModifier = false,
        DateTimeOffset? dateInscription = null)
    {
        ArgumentNullException.ThrowIfNull(pseudo);
        ArgumentNullException.ThrowIfNull(mail);
        ArgumentNullException.ThrowIfNull(motDePasse);

        return new Utilisateur
        {
            Pseudo = pseudo,
            Mail = mail,
            MotDePasse = motDePasse,
            Role = role,
            Suspendu = false,
            NecessaireAModifier = necessaireAModifier,
            DateInscription = dateInscription ?? DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Replaces the current password hash and clears any pending reset token.
    /// </summary>
    /// <param name="nouveauHash">The new password hash.</param>
    public void ChangerMotDePasse(MotDePasseHash nouveauHash)
    {
        ArgumentNullException.ThrowIfNull(nouveauHash);

        MotDePasse = nouveauHash;
        NecessaireAModifier = false;
        ResetToken = null;
        ResetTokenExpireLe = null;
    }

    /// <summary>
    /// Suspends the user, blocking subsequent logins.
    /// </summary>
    /// <exception cref="UtilisateurDejaSuspenduException">Thrown when the user is already suspended.</exception>
    public void Suspendre()
    {
        if (Suspendu)
        {
            throw new UtilisateurDejaSuspenduException();
        }

        Suspendu = true;
    }

    /// <summary>
    /// Reactivates a suspended user.
    /// </summary>
    /// <exception cref="UtilisateurNonSuspenduException">Thrown when the user is not currently suspended.</exception>
    public void Reactiver()
    {
        if (!Suspendu)
        {
            throw new UtilisateurNonSuspenduException();
        }

        Suspendu = false;
    }

    /// <summary>
    /// Records a successful login timestamp.
    /// </summary>
    /// <param name="quand">The login timestamp (UTC).</param>
    public void EnregistrerConnexion(DateTimeOffset quand)
    {
        DerniereConnexion = quand;
    }

    /// <summary>
    /// Stores a password reset token along with its expiry.
    /// </summary>
    /// <param name="token">The opaque reset token.</param>
    /// <param name="expireLe">The expiry timestamp (must be in the future).</param>
    /// <exception cref="ReinitialisationMotDePasseInvalideException">
    /// Thrown when the token is empty or the expiry is not strictly in the future.
    /// </exception>
    public void DemarrerReinitialisationMotDePasse(string token, DateTimeOffset expireLe)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ReinitialisationMotDePasseInvalideException("Reset token cannot be empty.");
        }

        if (expireLe <= DateTimeOffset.UtcNow)
        {
            throw new ReinitialisationMotDePasseInvalideException("Reset token expiry must be in the future.");
        }

        ResetToken = token;
        ResetTokenExpireLe = expireLe;
    }
}
