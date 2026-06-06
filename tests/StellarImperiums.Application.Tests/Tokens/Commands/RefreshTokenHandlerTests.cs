using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Tokens.Commands;
using StellarImperiums.Application.Tokens.Exceptions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Tokens.Commands;

public class RefreshTokenHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";
    private const string CurrentHash =
        "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90";
    private const string SuccessorHash =
        "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";

    private static readonly Guid Family = Guid.Parse("0193b6a0-0000-7000-8000-000000000001");

    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _handler = new RefreshTokenHandler(_refreshTokens, _users, _tokenService);
        _tokenService.HashRefreshToken("opaque-value").Returns(CurrentHash);
        _tokenService.CreateAccessToken(Arg.Any<User>())
            .Returns(new AccessTokenResult("new-access-token", 900));
        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenMaterial(
                "new-opaque-value", SuccessorHash, DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static RefreshToken CreateStoredToken(DateTimeOffset? expiresAt = null) =>
        RefreshToken.Create(
            42,
            CurrentHash,
            Family,
            expiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(-1));

    private static User CreateUser() =>
        User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));

    [Fact]
    public async Task Handle_WithActiveToken_RotatesAndReturnsNewTokens()
    {
        var stored = CreateStoredToken();
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(CreateUser());

        var result = await _handler.Handle(
            new RefreshTokenCommand("opaque-value"), CancellationToken.None);

        result.AccessToken.ShouldBe("new-access-token");
        result.RefreshTokenValue.ShouldBe("new-opaque-value");
        stored.ReplacedByTokenHash.ShouldBe(SuccessorHash);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == SuccessorHash && t.FamilyId == Family),
            Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsRejected()
    {
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithReplayedRotatedToken_RevokesWholeFamily()
    {
        var stored = CreateStoredToken();
        stored.Rotate(SuccessorHash, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow);
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));

        await _refreshTokens.Received(1).RevokeFamilyAsync(
            Family, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ThrowsRejectedWithoutFamilyRevocation()
    {
        var stored = CreateStoredToken();
        stored.Revoke(DateTimeOffset.UtcNow);
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));

        await _refreshTokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsRejected()
    {
        var stored = RefreshToken.Create(
            42, CurrentHash, Family,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(-31));
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithSuspendedUser_RevokesFamilyAndThrows()
    {
        var stored = CreateStoredToken();
        var user = CreateUser();
        user.Suspend();
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);

        await Should.ThrowAsync<UserSuspendedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));

        await _refreshTokens.Received(1).RevokeFamilyAsync(
            Family, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMissingUser_ThrowsRejected()
    {
        var stored = CreateStoredToken();
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));
    }
}
