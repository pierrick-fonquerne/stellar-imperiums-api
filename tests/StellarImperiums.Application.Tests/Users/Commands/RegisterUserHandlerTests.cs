using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Commands;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class RegisterUserHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_repository, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsUserAndReturnsResponse()
    {
        var command = new RegisterUserCommand("Cmdr_Vega", "vega@stellar.io", "P@ssword12345");
        var hash = PasswordHash.Create(ValidHash);

        _repository.UsernameExistsAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.EmailExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("P@ssword12345").Returns(hash);

        var response = await _handler.Handle(command, CancellationToken.None);

        response.Username.ShouldBe("Cmdr_Vega");
        response.Email.ShouldBe("vega@stellar.io");
        await _repository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Username.Value == "Cmdr_Vega"
            && u.Email.Value == "vega@stellar.io"
            && u.PasswordHash == hash), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyTaken_ThrowsAndSkipsPersistence()
    {
        var command = new RegisterUserCommand("Cmdr_Vega", "vega@stellar.io", "P@ssword12345");
        _repository.UsernameExistsAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Should.ThrowAsync<UsernameAlreadyTakenException>(
            () => _handler.Handle(command, CancellationToken.None));

        ex.Username.ShouldBe("Cmdr_Vega");
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ThrowsAndSkipsPersistence()
    {
        var command = new RegisterUserCommand("Cmdr_Vega", "vega@stellar.io", "P@ssword12345");
        _repository.UsernameExistsAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.EmailExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Should.ThrowAsync<EmailAlreadyTakenException>(
            () => _handler.Handle(command, CancellationToken.None));

        ex.Email.ShouldBe("vega@stellar.io");
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesEmailToLowercase()
    {
        var command = new RegisterUserCommand("Cmdr_Vega", "Vega@STELLAR.IO", "P@ssword12345");
        _repository.UsernameExistsAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.EmailExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(PasswordHash.Create(ValidHash));

        var response = await _handler.Handle(command, CancellationToken.None);

        response.Email.ShouldBe("vega@stellar.io");
    }
}
