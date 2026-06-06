using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Commands;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class LoginUserHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(_users, _refreshTokens, _passwordHasher, _tokenService);
        _tokenService.CreateAccessToken(Arg.Any<User>())
            .Returns(new AccessTokenResult("access-token", 900));
        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenMaterial(
                "refresh-value",
                "0011223344556677889900112233445566778899001122334455667788990011",
                DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static User CreateUser(bool mustChangePassword = false)
    {
        return User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash),
            mustChangePassword: mustChangePassword);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokensAndRecordsLogin()
    {
        var user = CreateUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("P@ssword12345", user.PasswordHash).Returns(true);

        var result = await _handler.Handle(
            new LoginUserCommand("vega@stellar.io", "P@ssword12345"), CancellationToken.None);

        result.AccessToken.ShouldBe("access-token");
        result.ExpiresInSeconds.ShouldBe(900);
        result.Username.ShouldBe("Cmdr_Vega");
        result.Email.ShouldBe("vega@stellar.io");
        result.Role.ShouldBe("Player");
        result.RefreshTokenValue.ShouldBe("refresh-value");
        result.MustChangePassword.ShouldBeFalse();
        user.LastLoginAt.ShouldNotBeNull();
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t =>
                t.TokenHash == "0011223344556677889900112233445566778899001122334455667788990011"
                && t.FamilyId != Guid.Empty),
            Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsGenericAndStillVerifiesPassword()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _handler.Handle(
                new LoginUserCommand("ghost@stellar.io", "P@ssword12345"), CancellationToken.None));

        _passwordHasher.Received(1).Verify("P@ssword12345", Arg.Any<PasswordHash>());
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsGenericAndCreatesNoToken()
    {
        var user = CreateUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong-password", user.PasswordHash).Returns(false);

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _handler.Handle(
                new LoginUserCommand("vega@stellar.io", "wrong-password"), CancellationToken.None));

        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSuspendedUser_ThrowsAndCreatesNoToken()
    {
        var user = CreateUser();
        user.Suspend();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("P@ssword12345", user.PasswordHash).Returns(true);

        await Should.ThrowAsync<UserSuspendedException>(
            () => _handler.Handle(
                new LoginUserCommand("vega@stellar.io", "P@ssword12345"), CancellationToken.None));

        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PropagatesMustChangePasswordFlag()
    {
        var user = CreateUser(mustChangePassword: true);
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("P@ssword12345", user.PasswordHash).Returns(true);

        var result = await _handler.Handle(
            new LoginUserCommand("vega@stellar.io", "P@ssword12345"), CancellationToken.None);

        result.MustChangePassword.ShouldBeTrue();
    }
}
