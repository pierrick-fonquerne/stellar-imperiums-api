using Shouldly;
using StellarImperiums.Application.Users.Commands;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        var command = new RegisterUserCommand("Cmdr_Vega", "vega@stellar.io", "P@ssword12345");

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    public void Validate_WithTooShortUsername_Fails(string username)
    {
        var command = new RegisterUserCommand(username, "vega@stellar.io", "P@ssword12345");

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserCommand.Username));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    [InlineData("")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var command = new RegisterUserCommand("Cmdr_Vega", email, "P@ssword12345");

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void Validate_WithTooShortPassword_Fails(string password)
    {
        var command = new RegisterUserCommand("Cmdr_Vega", "vega@stellar.io", password);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserCommand.PlaintextPassword));
    }

    [Fact]
    public void Validate_WithTooLongPassword_Fails()
    {
        var tooLong = new string('a', RegisterUserValidator.MaxPasswordLength + 1);
        var command = new RegisterUserCommand("Cmdr_Vega", "vega@stellar.io", tooLong);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserCommand.PlaintextPassword));
    }
}
