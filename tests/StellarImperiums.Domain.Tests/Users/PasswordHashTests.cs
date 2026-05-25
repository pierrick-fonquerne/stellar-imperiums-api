using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Tests.Users;

public class PasswordHashTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    [Fact]
    public void Create_WithValidPhcHash_ReturnsInstance()
    {
        var hash = PasswordHash.Create(ValidHash);

        hash.Value.ShouldBe(ValidHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_Throws(string? value)
    {
        Should.Throw<InvalidPasswordHashException>(() => PasswordHash.Create(value!));
    }

    [Theory]
    [InlineData("$2b$12$abcdef...")]
    [InlineData("$argon2i$v=19$m=65536,t=3,p=4$abc$def")]
    [InlineData("plaintext_password")]
    [InlineData("$pbkdf2-sha256$i=29000,l=32$abc$def")]
    public void Create_WithWrongAlgorithm_Throws(string value)
    {
        var ex = Should.Throw<InvalidPasswordHashException>(() => PasswordHash.Create(value));
        ex.Message.ShouldContain("argon2id");
    }

    [Fact]
    public void Create_WithHashTooLong_Throws()
    {
        var tooLong = $"$argon2id${new string('a', PasswordHash.MaxLength)}";

        var ex = Should.Throw<InvalidPasswordHashException>(() => PasswordHash.Create(tooLong));
        ex.Message.ShouldContain("at most");
    }

    [Fact]
    public void ToString_DoesNotRevealTheHash()
    {
        var hash = PasswordHash.Create(ValidHash);

        hash.ToString().ShouldBe("***");
        hash.ToString().ShouldNotContain("argon2id");
    }
}
