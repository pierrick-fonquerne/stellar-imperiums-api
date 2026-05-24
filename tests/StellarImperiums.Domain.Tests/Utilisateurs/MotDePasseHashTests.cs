using Shouldly;
using StellarImperiums.Domain.Utilisateurs;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Tests.Utilisateurs;

public class MotDePasseHashTests
{
    private const string HashValide =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    [Fact]
    public void Creer_AvecHashPhcValide_RetourneInstance()
    {
        var hash = MotDePasseHash.Creer(HashValide);

        hash.Valeur.ShouldBe(HashValide);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_AvecValeurVide_Throws(string? valeur)
    {
        Should.Throw<MotDePasseHashInvalideException>(() => MotDePasseHash.Creer(valeur!));
    }

    [Theory]
    [InlineData("$2b$12$abcdef...")]
    [InlineData("$argon2i$v=19$m=65536,t=3,p=4$abc$def")]
    [InlineData("plaintext_password")]
    [InlineData("$pbkdf2-sha256$i=29000,l=32$abc$def")]
    public void Creer_AvecAlgorithmeIncorrect_Throws(string valeur)
    {
        var ex = Should.Throw<MotDePasseHashInvalideException>(() => MotDePasseHash.Creer(valeur));
        ex.Message.ShouldContain("argon2id");
    }

    [Fact]
    public void Creer_AvecHashTropLong_Throws()
    {
        var tooLong = $"$argon2id${new string('a', MotDePasseHash.LongueurMax)}";

        var ex = Should.Throw<MotDePasseHashInvalideException>(() => MotDePasseHash.Creer(tooLong));
        ex.Message.ShouldContain("at most");
    }

    [Fact]
    public void ToString_NeRevelePasLeHash()
    {
        var hash = MotDePasseHash.Creer(HashValide);

        hash.ToString().ShouldBe("***");
        hash.ToString().ShouldNotContain("argon2id");
    }
}
