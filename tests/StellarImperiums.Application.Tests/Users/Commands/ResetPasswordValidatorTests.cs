using Shouldly;
using StellarImperiums.Application.Users.Commands;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    private static string ValidPassword => new('a', RegisterUserValidator.MinPasswordLength);

    [Fact]
    public void Validate_WithEmptyToken_Fails()
    {
        var command = new ResetPasswordCommand(string.Empty, ValidPassword);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ResetPasswordCommand.Token));
    }

    [Fact]
    public void Validate_WithTooShortPassword_Fails()
    {
        var tooShort = new string('a', RegisterUserValidator.MinPasswordLength - 1);
        var command = new ResetPasswordCommand("some-token", tooShort);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithTooLongPassword_Fails()
    {
        var tooLong = new string('a', RegisterUserValidator.MaxPasswordLength + 1);
        var command = new ResetPasswordCommand("some-token", tooLong);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithValidTokenAndPassword_Passes()
    {
        var command = new ResetPasswordCommand("some-valid-token", ValidPassword);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
