using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Tests.Users;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("a.b+tag@sub.domain.io")]
    [InlineData("pierrick.fonquerne@nubster.com")]
    public void Create_WithValidValue_ReturnsInstance(string value)
    {
        var email = Email.Create(value);

        email.Value.ShouldBe(value.ToLowerInvariant());
    }

    [Fact]
    public void Create_WithUpperCaseAndWhitespace_Normalizes()
    {
        var email = Email.Create("  USER@EXAMPLE.COM  ");

        email.Value.ShouldBe("user@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_Throws(string? value)
    {
        Should.Throw<InvalidEmailException>(() => Email.Create(value!));
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@no-local.com")]
    [InlineData("missing-tld@domain")]
    [InlineData("two@@signs.com")]
    [InlineData("space in@local.com")]
    public void Create_WithInvalidFormat_Throws(string value)
    {
        Should.Throw<InvalidEmailException>(() => Email.Create(value));
    }

    [Fact]
    public void Create_WithTooLongValue_Throws()
    {
        var local = new string('a', Email.MaxLength);
        var tooLong = $"{local}@domain.com";

        var ex = Should.Throw<InvalidEmailException>(() => Email.Create(tooLong));
        ex.Message.ShouldContain("at most");
    }

    [Fact]
    public void Equality_NormalizationEnsuresEquality()
    {
        var a = Email.Create("USER@EXAMPLE.COM");
        var b = Email.Create("user@example.com");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }
}
