using Shouldly;
using StellarImperiums.Domain.Utilisateurs;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Tests.Utilisateurs;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("a.b+tag@sub.domain.io")]
    [InlineData("pierrick.fonquerne@nubster.com")]
    public void Creer_AvecValeurValide_RetourneInstance(string valeur)
    {
        var email = Email.Creer(valeur);

        email.Valeur.ShouldBe(valeur.ToLowerInvariant());
    }

    [Fact]
    public void Creer_AvecCasseEtEspaces_Normalise()
    {
        var email = Email.Creer("  USER@EXAMPLE.COM  ");

        email.Valeur.ShouldBe("user@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecValeurVide_Throws(string? valeur)
    {
        Should.Throw<EmailInvalideException>(() => Email.Creer(valeur!));
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@no-local.com")]
    [InlineData("missing-tld@domain")]
    [InlineData("two@@signs.com")]
    [InlineData("space in@local.com")]
    public void Creer_AvecFormatInvalide_Throws(string valeur)
    {
        Should.Throw<EmailInvalideException>(() => Email.Creer(valeur));
    }

    [Fact]
    public void Creer_AvecValeurTropLongue_Throws()
    {
        var local = new string('a', Email.LongueurMax);
        var tooLong = $"{local}@domain.com";

        var ex = Should.Throw<EmailInvalideException>(() => Email.Creer(tooLong));
        ex.Message.ShouldContain("at most");
    }

    [Fact]
    public void Equality_NormalisationGarantitEgalite()
    {
        var a = Email.Creer("USER@EXAMPLE.COM");
        var b = Email.Creer("user@example.com");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }
}
