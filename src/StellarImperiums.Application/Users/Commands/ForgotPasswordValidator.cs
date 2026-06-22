using FluentValidation;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// FluentValidation rules applied to <see cref="ForgotPasswordCommand"/> before the handler runs.
/// </summary>
public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordValidator"/> class.
    /// </summary>
    public ForgotPasswordValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(Email.MaxLength).WithMessage($"Email must be at most {Email.MaxLength} characters.");
    }
}
