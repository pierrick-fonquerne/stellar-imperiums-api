using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Commands;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class ResetPasswordHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";
    private const string NewHash =
        "$argon2id$v=19$m=65536,t=3,p=4$1FCks4yDhpw2PqOmKg7FAw$FjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ResetPasswordHandler _handler;

    public ResetPasswordHandlerTests()
    {
        _handler = new ResetPasswordHandler(_users, _refreshTokens, _passwordHasher, _tokenService);
    }

    private static User CreateUserWithToken(string tokenHash, DateTimeOffset expiresAt)
    {
        var user = User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));
        user.StartPasswordReset(tokenHash, expiresAt);
        return user;
    }

    private static User CreateFreshUser()
    {
        return User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));
    }

    [Fact]
    public async Task Handle_WithValidToken_ChangesPasswordAndRevokesRefreshTokens()
    {
        var newHash = PasswordHash.Create(NewHash);
        var user = CreateUserWithToken("TOKEN-HASH", DateTimeOffset.UtcNow.AddHours(1));
        _tokenService.HashPasswordResetToken("plain-token").Returns("TOKEN-HASH");
        _users.GetByPasswordResetTokenAsync("TOKEN-HASH", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Hash("NewP@ssword123").Returns(newHash);

        await _handler.Handle(new ResetPasswordCommand("plain-token", "NewP@ssword123"), CancellationToken.None);

        user.PasswordHash.ShouldBe(newHash);
        user.PasswordResetToken.ShouldBeNull();
        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsInvalidPasswordResetTokenException()
    {
        _tokenService.HashPasswordResetToken(Arg.Any<string>()).Returns("UNKNOWN-HASH");
        _users.GetByPasswordResetTokenAsync("UNKNOWN-HASH", Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<InvalidPasswordResetTokenException>(
            () => _handler.Handle(new ResetPasswordCommand("unknown-token", "NewP@ssword123"), CancellationToken.None));

        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().RevokeAllForUserAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNoLongerActive_ThrowsInvalidPasswordResetTokenException()
    {
        var user = CreateFreshUser();
        var originalHash = user.PasswordHash;
        _tokenService.HashPasswordResetToken("stale-plain").Returns("STALE-HASH");
        _users.GetByPasswordResetTokenAsync("STALE-HASH", Arg.Any<CancellationToken>()).Returns(user);

        await Should.ThrowAsync<InvalidPasswordResetTokenException>(
            () => _handler.Handle(new ResetPasswordCommand("stale-plain", "NewP@ssword123"), CancellationToken.None));

        user.PasswordHash.ShouldBe(originalHash);
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().RevokeAllForUserAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
