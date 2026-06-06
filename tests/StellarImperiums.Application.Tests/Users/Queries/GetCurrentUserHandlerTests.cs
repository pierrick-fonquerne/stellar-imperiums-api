using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Application.Users.Queries;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Queries;

public class GetCurrentUserHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests()
    {
        _handler = new GetCurrentUserHandler(_users);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsProfile()
    {
        var user = User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetCurrentUserQuery(42), CancellationToken.None);

        result.Username.ShouldBe("Cmdr_Vega");
        result.Email.ShouldBe("vega@stellar.io");
        result.Role.ShouldBe("Player");
        result.RegistrationDate.ShouldBe(user.RegistrationDate);
        result.LastLoginAt.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithUnknownUser_Throws()
    {
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<UserNotFoundException>(
            () => _handler.Handle(new GetCurrentUserQuery(42), CancellationToken.None));
    }
}
