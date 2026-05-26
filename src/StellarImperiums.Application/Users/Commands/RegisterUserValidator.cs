using FluentValidation;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// FluentValidation rules applied to <see cref="RegisterUserCommand"/> before the handler runs.
/// </summary>
/// <remarks>
/// These rules return friendly error messages early. Stricter domain invariants (regex email, PHC format)
/// are enforced again by the value objects when the entity is created.
/// </remarks>
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Minimum password length required at the application boundary.
    /// </summary>
    public const int MinPasswordLength = 12;

    /// <summary>
    /// Maximum password length to prevent denial-of-service via overly large argon2 inputs.
    /// </summary>
    public const int MaxPasswordLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserValidator"/> class.
    /// </summary>
    public RegisterUserValidator()
    {
        RuleFor(c => c.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(Username.MinLength).WithMessage($"Username must be at least {Username.MinLength} characters.")
            .MaximumLength(Username.MaxLength).WithMessage($"Username must be at most {Username.MaxLength} characters.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(Email.MaxLength).WithMessage($"Email must be at most {Email.MaxLength} characters.");

        RuleFor(c => c.PlaintextPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinPasswordLength).WithMessage($"Password must be at least {MinPasswordLength} characters.")
            .MaximumLength(MaxPasswordLength).WithMessage($"Password must be at most {MaxPasswordLength} characters.");
    }
}
