using FluentValidation;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// FluentValidation rules applied to <see cref="LoginUserCommand"/> before the handler runs.
/// </summary>
/// <remarks>
/// No minimum password length is enforced here: revealing the password policy on the login
/// endpoint would leak information. The maximum guards against denial-of-service through
/// oversized argon2 inputs.
/// </remarks>
public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginUserValidator"/> class.
    /// </summary>
    public LoginUserValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(Email.MaxLength).WithMessage($"Email must be at most {Email.MaxLength} characters.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(RegisterUserValidator.MaxPasswordLength)
            .WithMessage($"Password must be at most {RegisterUserValidator.MaxPasswordLength} characters.");
    }
}
