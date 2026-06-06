using Shouldly;
using StellarImperiums.Application.Users.Commands;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class LoginUserValidatorTests
{
    private readonly LoginUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new LoginUserCommand("vega@stellar.io", "P@ssword12345"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(new LoginUserCommand(email, "P@ssword12345"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginUserCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_Fails()
    {
        var result = _validator.Validate(new LoginUserCommand("vega@stellar.io", ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginUserCommand.Password));
    }

    [Fact]
    public void Validate_WithOverlongPassword_Fails()
    {
        var result = _validator.Validate(
            new LoginUserCommand("vega@stellar.io", new string('a', 129)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginUserCommand.Password));
    }

    [Fact]
    public void Validate_DoesNotEnforceMinimumPasswordLength()
    {
        var result = _validator.Validate(new LoginUserCommand("vega@stellar.io", "abc"));

        result.IsValid.ShouldBeTrue();
    }
}
