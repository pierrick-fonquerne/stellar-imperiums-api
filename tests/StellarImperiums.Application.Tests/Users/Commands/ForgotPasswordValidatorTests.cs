using Shouldly;
using StellarImperiums.Application.Users.Commands;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class ForgotPasswordValidatorTests
{
    private readonly ForgotPasswordValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyEmail_Fails()
    {
        var command = new ForgotPasswordCommand(string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ForgotPasswordCommand.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    [InlineData("missing-at-sign")]
    public void Validate_WithMalformedEmail_Fails(string email)
    {
        var command = new ForgotPasswordCommand(email);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ForgotPasswordCommand.Email));
    }

    [Fact]
    public void Validate_WithValidEmail_Passes()
    {
        var command = new ForgotPasswordCommand("vega@stellar.io");

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
