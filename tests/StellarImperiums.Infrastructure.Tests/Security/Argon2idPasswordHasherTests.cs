using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Infrastructure.Security;

namespace StellarImperiums.Infrastructure.Tests.Security;

public class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsPhcFormattedString()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple!42");

        hash.Value.ShouldStartWith("$argon2id$v=19$m=65536,t=3,p=4$");
        hash.Value.Split('$').Length.ShouldBe(6);
    }

    [Fact]
    public void Hash_TwoInvocations_ProduceDifferentSalts()
    {
        var hashA = _hasher.Hash("CorrectHorseBatteryStaple!42");
        var hashB = _hasher.Hash("CorrectHorseBatteryStaple!42");

        hashA.Value.ShouldNotBe(hashB.Value);
    }

    [Fact]
    public void Verify_WithMatchingPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple!42");

        _hasher.Verify("CorrectHorseBatteryStaple!42", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple!42");

        _hasher.Verify("WrongPassword!", hash).ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithSimilarPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple!42");

        _hasher.Verify("CorrectHorseBatteryStaple!43", hash).ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        var bogusHash = PasswordHash.Create("$argon2id$not-a-real-hash");

        _hasher.Verify("anything", bogusHash).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_WithEmptyPassword_Throws(string? password)
    {
        Should.Throw<ArgumentException>(() => _hasher.Hash(password!));
    }
}
