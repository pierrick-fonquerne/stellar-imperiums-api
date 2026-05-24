using Shouldly;
using StellarImperiums.Domain.Utilisateurs;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Tests.Utilisateurs;

public class UtilisateurTests
{
    private const string HashValide =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private static Utilisateur CreerUtilisateurStandard() => Utilisateur.Creer(
        Pseudo.Creer("Cmdr_Vega"),
        Email.Creer("vega@stellar.io"),
        MotDePasseHash.Creer(HashValide));

    [Fact]
    public void Creer_AvecDonneesValides_InitialiseLesProprietes()
    {
        var avant = DateTimeOffset.UtcNow;
        var utilisateur = CreerUtilisateurStandard();
        var apres = DateTimeOffset.UtcNow;

        utilisateur.Pseudo.Valeur.ShouldBe("Cmdr_Vega");
        utilisateur.Mail.Valeur.ShouldBe("vega@stellar.io");
        utilisateur.Role.ShouldBe(RoleUtilisateur.Joueur);
        utilisateur.Suspendu.ShouldBeFalse();
        utilisateur.NecessaireAModifier.ShouldBeFalse();
        utilisateur.DerniereConnexion.ShouldBeNull();
        utilisateur.ResetToken.ShouldBeNull();
        utilisateur.ResetTokenExpireLe.ShouldBeNull();
        utilisateur.DateInscription.ShouldBeInRange(avant, apres);
    }

    [Fact]
    public void Creer_AvecRoleAdmin_AssigneLeRole()
    {
        var admin = Utilisateur.Creer(
            Pseudo.Creer("admin"),
            Email.Creer("admin@stellar.io"),
            MotDePasseHash.Creer(HashValide),
            role: RoleUtilisateur.Admin,
            necessaireAModifier: true);

        admin.Role.ShouldBe(RoleUtilisateur.Admin);
        admin.NecessaireAModifier.ShouldBeTrue();
    }

    [Fact]
    public void Creer_AvecArgumentsNuls_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Utilisateur.Creer(null!, Email.Creer("a@b.io"), MotDePasseHash.Creer(HashValide)));
        Should.Throw<ArgumentNullException>(() => Utilisateur.Creer(Pseudo.Creer("abc"), null!, MotDePasseHash.Creer(HashValide)));
        Should.Throw<ArgumentNullException>(() => Utilisateur.Creer(Pseudo.Creer("abc"), Email.Creer("a@b.io"), null!));
    }

    [Fact]
    public void ChangerMotDePasse_MetAJourLeHashEtReinitialiseLesFlags()
    {
        var utilisateur = Utilisateur.Creer(
            Pseudo.Creer("Cmdr_Vega"),
            Email.Creer("vega@stellar.io"),
            MotDePasseHash.Creer(HashValide),
            necessaireAModifier: true);

        utilisateur.DemarrerReinitialisationMotDePasse("token-abc", DateTimeOffset.UtcNow.AddHours(1));

        var nouveau = MotDePasseHash.Creer(HashValide.Replace("EjBGRNLgHES", "ZzZzZzZzZzZ"));
        utilisateur.ChangerMotDePasse(nouveau);

        utilisateur.MotDePasse.ShouldBe(nouveau);
        utilisateur.NecessaireAModifier.ShouldBeFalse();
        utilisateur.ResetToken.ShouldBeNull();
        utilisateur.ResetTokenExpireLe.ShouldBeNull();
    }

    [Fact]
    public void Suspendre_PassageDeActifASuspendu_OK()
    {
        var utilisateur = CreerUtilisateurStandard();

        utilisateur.Suspendre();

        utilisateur.Suspendu.ShouldBeTrue();
    }

    [Fact]
    public void Suspendre_DejaSuspendu_Throws()
    {
        var utilisateur = CreerUtilisateurStandard();
        utilisateur.Suspendre();

        Should.Throw<UtilisateurDejaSuspenduException>(() => utilisateur.Suspendre());
    }

    [Fact]
    public void Reactiver_PassageDeSuspenduAActif_OK()
    {
        var utilisateur = CreerUtilisateurStandard();
        utilisateur.Suspendre();

        utilisateur.Reactiver();

        utilisateur.Suspendu.ShouldBeFalse();
    }

    [Fact]
    public void Reactiver_NonSuspendu_Throws()
    {
        var utilisateur = CreerUtilisateurStandard();

        Should.Throw<UtilisateurNonSuspenduException>(() => utilisateur.Reactiver());
    }

    [Fact]
    public void EnregistrerConnexion_MetAJourDerniereConnexion()
    {
        var utilisateur = CreerUtilisateurStandard();
        var quand = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);

        utilisateur.EnregistrerConnexion(quand);

        utilisateur.DerniereConnexion.ShouldBe(quand);
    }

    [Fact]
    public void DemarrerReinitialisationMotDePasse_AvecTokenValide_StockeLesValeurs()
    {
        var utilisateur = CreerUtilisateurStandard();
        var expireLe = DateTimeOffset.UtcNow.AddHours(1);

        utilisateur.DemarrerReinitialisationMotDePasse("token-xyz", expireLe);

        utilisateur.ResetToken.ShouldBe("token-xyz");
        utilisateur.ResetTokenExpireLe.ShouldBe(expireLe);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DemarrerReinitialisationMotDePasse_AvecTokenVide_Throws(string? token)
    {
        var utilisateur = CreerUtilisateurStandard();

        Should.Throw<ReinitialisationMotDePasseInvalideException>(() =>
            utilisateur.DemarrerReinitialisationMotDePasse(token!, DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public void DemarrerReinitialisationMotDePasse_AvecExpiryPasse_Throws()
    {
        var utilisateur = CreerUtilisateurStandard();

        Should.Throw<ReinitialisationMotDePasseInvalideException>(() =>
            utilisateur.DemarrerReinitialisationMotDePasse("token", DateTimeOffset.UtcNow.AddMinutes(-1)));
    }
}
