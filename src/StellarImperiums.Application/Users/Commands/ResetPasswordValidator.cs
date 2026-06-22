using FluentValidation;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// FluentValidation rules applied to <see cref="ResetPasswordCommand"/> before the handler runs.
/// </summary>
public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordValidator"/> class.
    /// </summary>
    public ResetPasswordValidator()
    {
        RuleFor(c => c.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(c => c.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(RegisterUserValidator.MinPasswordLength)
            .WithMessage($"New password must be at least {RegisterUserValidator.MinPasswordLength} characters.")
            .MaximumLength(RegisterUserValidator.MaxPasswordLength)
            .WithMessage($"New password must be at most {RegisterUserValidator.MaxPasswordLength} characters.");
    }
}
