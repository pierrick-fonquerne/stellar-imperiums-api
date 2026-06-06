using Shouldly;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Tokens.Exceptions;

namespace StellarImperiums.Domain.Tests.Tokens;

public class RefreshTokenTests
{
    private const string Hash = "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90";
    private const string NewHash = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";
    private const string ThirdHash = "FFEEDDCCBBAA99887766554433221100FFEEDDCCBBAA99887766554433221100";

    private static readonly DateTimeOffset Now = new(2026, 6, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Family = Guid.Parse("0193b6a0-0000-7000-8000-000000000001");

    private static RefreshToken CreateToken(
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? createdAt = null) =>
        RefreshToken.Create(42, Hash, Family, expiresAt ?? Now.AddDays(30), createdAt ?? Now);

    [Fact]
    public void Create_WithValidArguments_SetsProperties()
    {
        var token = CreateToken();

        token.UserId.ShouldBe(42);
        token.TokenHash.ShouldBe(Hash);
        token.FamilyId.ShouldBe(Family);
        token.CreatedAt.ShouldBe(Now);
        token.ExpiresAt.ShouldBe(Now.AddDays(30));
        token.RevokedAt.ShouldBeNull();
        token.ReplacedByTokenHash.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyHash_Throws(string emptyHash)
    {
        Should.Throw<InvalidRefreshTokenException>(
            () => RefreshToken.Create(42, emptyHash, Family, Now.AddDays(30), Now));
    }

    [Fact]
    public void Create_WithEmptyFamilyId_Throws()
    {
        Should.Throw<InvalidRefreshTokenException>(
            () => RefreshToken.Create(42, Hash, Guid.Empty, Now.AddDays(30), Now));
    }

    [Fact]
    public void Create_WithExpiryNotAfterCreation_Throws()
    {
        Should.Throw<InvalidRefreshTokenException>(
            () => RefreshToken.Create(42, Hash, Family, Now, Now));
    }

    [Fact]
    public void IsActive_WhenFreshlyCreated_IsTrue()
    {
        CreateToken().IsActive(Now.AddDays(1)).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_IsFalse()
    {
        var token = CreateToken(expiresAt: Now.AddDays(1));

        token.IsExpired(Now.AddDays(2)).ShouldBeTrue();
        token.IsActive(Now.AddDays(2)).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_IsFalse()
    {
        var token = CreateToken();
        token.Revoke(Now.AddHours(1));

        token.IsRevoked.ShouldBeTrue();
        token.IsActive(Now.AddHours(2)).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenRotated_IsFalse()
    {
        var token = CreateToken();
        token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1));

        token.IsRotated.ShouldBeTrue();
        token.IsActive(Now.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void Rotate_ReturnsSuccessorInSameFamily_AndMarksCurrentAsRotated()
    {
        var token = CreateToken();

        var successor = token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1));

        successor.TokenHash.ShouldBe(NewHash);
        successor.FamilyId.ShouldBe(Family);
        successor.UserId.ShouldBe(42);
        successor.CreatedAt.ShouldBe(Now.AddDays(1));
        successor.ExpiresAt.ShouldBe(Now.AddDays(31));
        token.ReplacedByTokenHash.ShouldBe(NewHash);
    }

    [Fact]
    public void Rotate_WhenRevoked_Throws()
    {
        var token = CreateToken();
        token.Revoke(Now.AddHours(1));

        Should.Throw<RefreshTokenRevokedException>(
            () => token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1)));
    }

    [Fact]
    public void Rotate_WhenAlreadyRotated_Throws()
    {
        var token = CreateToken();
        token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1));

        Should.Throw<RefreshTokenReuseException>(
            () => token.Rotate(ThirdHash, Now.AddDays(31), Now.AddDays(2)));
    }

    [Fact]
    public void Rotate_WhenExpired_Throws()
    {
        var token = CreateToken(expiresAt: Now.AddDays(1));

        Should.Throw<RefreshTokenExpiredException>(
            () => token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(2)));
    }

    [Fact]
    public void Revoke_SetsRevokedAt()
    {
        var token = CreateToken();

        token.Revoke(Now.AddHours(1));

        token.RevokedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Revoke_CalledTwice_KeepsFirstTimestamp()
    {
        var token = CreateToken();
        token.Revoke(Now.AddHours(1));

        token.Revoke(Now.AddHours(5));

        token.RevokedAt.ShouldBe(Now.AddHours(1));
    }
}
