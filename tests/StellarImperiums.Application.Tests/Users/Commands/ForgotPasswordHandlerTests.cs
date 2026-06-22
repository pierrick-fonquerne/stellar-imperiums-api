using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Commands;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class ForgotPasswordHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ForgotPasswordHandler _handler;

    public ForgotPasswordHandlerTests()
    {
        _handler = new ForgotPasswordHandler(_users, _tokenService, _emailSender);
    }

    private static User CreateUser() =>
        User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));

    [Fact]
    public async Task Handle_ForExistingUser_GeneratesTokenPersistsAndSendsEmail()
    {
        var user = CreateUser();
        var email = Email.Create("vega@stellar.io");
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _tokenService.GeneratePasswordResetToken()
            .Returns(new PasswordResetTokenMaterial("plain-value", "HASH-VALUE"));

        await _handler.Handle(new ForgotPasswordCommand("vega@stellar.io"), CancellationToken.None);

        _tokenService.Received(1).GeneratePasswordResetToken();
        user.PasswordResetToken.ShouldBe("HASH-VALUE");
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendPasswordResetAsync(
            Arg.Is<Email>(e => e.Value == "vega@stellar.io"),
            "plain-value",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForUnknownEmail_DoesNothingAndThrowsNothing()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _handler.Handle(new ForgotPasswordCommand("ghost@stellar.io"), CancellationToken.None);

        _tokenService.DidNotReceive().GeneratePasswordResetToken();
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
