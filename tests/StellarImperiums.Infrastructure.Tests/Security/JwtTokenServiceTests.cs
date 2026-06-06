using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Infrastructure.Security;
using System.Text;

namespace StellarImperiums.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private const string SigningKey = "unit-tests-signing-key-0123456789abcdef0123456789abcdef";
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        Issuer = "StellarImperiums.Tests",
        Audience = "StellarImperiums.Tests",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
        SigningKey = SigningKey
    }));

    private static User CreateUser() =>
        User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));

    [Fact]
    public void CreateAccessToken_EmbedsExpectedClaims()
    {
        var result = _service.CreateAccessToken(CreateUser());

        var token = new JsonWebToken(result.Token);
        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe("0");
        token.GetClaim(JwtRegisteredClaimNames.UniqueName).Value.ShouldBe("Cmdr_Vega");
        token.GetClaim("role").Value.ShouldBe("Player");
        token.TryGetClaim(JwtRegisteredClaimNames.Jti, out _).ShouldBeTrue();
        token.Issuer.ShouldBe("StellarImperiums.Tests");
        token.Audiences.ShouldContain("StellarImperiums.Tests");
        token.Alg.ShouldBe("HS256");
    }

    [Fact]
    public void CreateAccessToken_DoesNotEmbedEmail()
    {
        var result = _service.CreateAccessToken(CreateUser());

        var token = new JsonWebToken(result.Token);
        token.TryGetClaim(JwtRegisteredClaimNames.Email, out _).ShouldBeFalse();
        result.Token.ShouldNotContain("vega@stellar.io");
    }

    [Fact]
    public void CreateAccessToken_SetsFifteenMinutesLifetime()
    {
        var result = _service.CreateAccessToken(CreateUser());

        result.ExpiresInSeconds.ShouldBe(900);
        var token = new JsonWebToken(result.Token);
        (token.ValidTo - token.ValidFrom).ShouldBe(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task CreateAccessToken_SignatureValidatesWithTheConfiguredKey()
    {
        var result = _service.CreateAccessToken(CreateUser());

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.Token,
            new TokenValidationParameters
            {
                ValidIssuer = "StellarImperiums.Tests",
                ValidAudience = "StellarImperiums.Tests",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidAlgorithms = ["HS256"],
                ValidateLifetime = true
            });

        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesHighEntropyValueAndHexHash()
    {
        var material = _service.GenerateRefreshToken();

        material.Value.Length.ShouldBe(43);
        material.Hash.Length.ShouldBe(64);
        material.Hash.ShouldAllBe(c => Uri.IsHexDigit(c));
        material.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddDays(29));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueValues()
    {
        var first = _service.GenerateRefreshToken();
        var second = _service.GenerateRefreshToken();

        first.Value.ShouldNotBe(second.Value);
        first.Hash.ShouldNotBe(second.Hash);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministicAndMatchesGeneratedMaterial()
    {
        var material = _service.GenerateRefreshToken();

        _service.HashRefreshToken(material.Value).ShouldBe(material.Hash);
        _service.HashRefreshToken(material.Value).ShouldBe(_service.HashRefreshToken(material.Value));
    }
}
