using NSubstitute;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Tokens.Commands;
using StellarImperiums.Domain.Tokens;

namespace StellarImperiums.Application.Tests.Tokens.Commands;

public class LogoutHandlerTests
{
    private const string CurrentHash =
        "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90";

    private static readonly Guid Family = Guid.Parse("0193b6a0-0000-7000-8000-000000000001");

    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _handler = new LogoutHandler(_refreshTokens, _tokenService);
        _tokenService.HashRefreshToken("opaque-value").Returns(CurrentHash);
    }

    [Fact]
    public async Task Handle_WithKnownToken_RevokesFamily()
    {
        var stored = RefreshToken.Create(
            42, CurrentHash, Family, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow.AddDays(-1));
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await _handler.Handle(new LogoutCommand("opaque-value"), CancellationToken.None);

        await _refreshTokens.Received(1).RevokeFamilyAsync(
            Family, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_DoesNothing()
    {
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        await _handler.Handle(new LogoutCommand("opaque-value"), CancellationToken.None);

        await _refreshTokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithMissingTokenValue_DoesNothing(string? value)
    {
        await _handler.Handle(new LogoutCommand(value), CancellationToken.None);

        _tokenService.DidNotReceive().HashRefreshToken(Arg.Any<string>());
        await _refreshTokens.DidNotReceive().GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
