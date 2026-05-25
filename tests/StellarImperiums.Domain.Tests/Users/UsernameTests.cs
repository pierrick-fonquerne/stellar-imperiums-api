using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Tests.Users;

public class UsernameTests
{
    [Theory]
    [InlineData("Cmdr_Vega")]
    [InlineData("abc")]
    [InlineData("StellarRider_42")]
    public void Create_WithValidValue_ReturnsInstance(string value)
    {
        var username = Username.Create(value);

        username.Value.ShouldBe(value);
        username.ToString().ShouldBe(value);
    }

    [Fact]
    public void Create_WithLeadingTrailingWhitespace_TrimsTheValue()
    {
        var username = Username.Create("  Vega  ");

        username.Value.ShouldBe("Vega");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_Throws(string? value)
    {
        Should.Throw<InvalidUsernameException>(() => Username.Create(value!));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("x")]
    public void Create_WithTooShortValue_Throws(string value)
    {
        var ex = Should.Throw<InvalidUsernameException>(() => Username.Create(value));
        ex.Message.ShouldContain("at least");
    }

    [Fact]
    public void Create_WithTooLongValue_Throws()
    {
        var tooLong = new string('a', Username.MaxLength + 1);

        var ex = Should.Throw<InvalidUsernameException>(() => Username.Create(tooLong));
        ex.Message.ShouldContain("at most");
    }

    [Fact]
    public void Equality_IdenticalUsernames_AreEqual()
    {
        var a = Username.Create("Vega");
        var b = Username.Create("Vega");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentUsernames_AreNotEqual()
    {
        var a = Username.Create("Vega");
        var b = Username.Create("Nova");

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }
}
