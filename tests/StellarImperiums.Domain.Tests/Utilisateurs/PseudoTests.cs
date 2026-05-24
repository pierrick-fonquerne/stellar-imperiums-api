using Shouldly;
using StellarImperiums.Domain.Utilisateurs;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Tests.Utilisateurs;

public class PseudoTests
{
    [Theory]
    [InlineData("Cmdr_Vega")]
    [InlineData("abc")]
    [InlineData("StellarRider_42")]
    public void Creer_AvecValeurValide_RetourneInstance(string valeur)
    {
        var pseudo = Pseudo.Creer(valeur);

        pseudo.Valeur.ShouldBe(valeur);
        pseudo.ToString().ShouldBe(valeur);
    }

    [Fact]
    public void Creer_AvecEspacesEnTrop_LesSupprime()
    {
        var pseudo = Pseudo.Creer("  Vega  ");

        pseudo.Valeur.ShouldBe("Vega");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecValeurVide_Throws(string? valeur)
    {
        Should.Throw<PseudoInvalideException>(() => Pseudo.Creer(valeur!));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("x")]
    public void Creer_AvecValeurTropCourte_Throws(string valeur)
    {
        var ex = Should.Throw<PseudoInvalideException>(() => Pseudo.Creer(valeur));
        ex.Message.ShouldContain("at least");
    }

    [Fact]
    public void Creer_AvecValeurTropLongue_Throws()
    {
        var tooLong = new string('a', Pseudo.LongueurMax + 1);

        var ex = Should.Throw<PseudoInvalideException>(() => Pseudo.Creer(tooLong));
        ex.Message.ShouldContain("at most");
    }

    [Fact]
    public void Equality_DeuxPseudosIdentiques_SontEgaux()
    {
        var a = Pseudo.Creer("Vega");
        var b = Pseudo.Creer("Vega");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_DeuxPseudosDifferents_NeSontPasEgaux()
    {
        var a = Pseudo.Creer("Vega");
        var b = Pseudo.Creer("Nova");

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }
}
