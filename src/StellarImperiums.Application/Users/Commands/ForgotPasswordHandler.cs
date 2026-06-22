using StellarImperiums.Application.Abstractions;
using DomainEmail = StellarImperiums.Domain.Users.Email;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Wolverine handler that initiates a password reset for the user with the given email address.
/// </summary>
/// <remarks>
/// When the email is unknown the handler returns without error or side effect
/// to prevent user enumeration through response timing or content differences.
/// The handler is intentionally neutral: callers cannot distinguish between
/// an unknown address and a successful dispatch.
/// </remarks>
public sealed class ForgotPasswordHandler(
    IUserRepository users,
    ITokenService tokenService,
    IEmailSender emailSender)
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// Handles the forgot-password command.
    /// </summary>
    /// <param name="command">The forgot-password command carrying the email address.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>
    /// Returns silently when the email is not associated with any account,
    /// ensuring that the response cannot be used to enumerate registered addresses.
    /// </remarks>
    public async Task Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = DomainEmail.Create(command.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return;
        }

        var material = tokenService.GeneratePasswordResetToken();
        var now = DateTimeOffset.UtcNow;
        user.StartPasswordReset(material.Hash, now.Add(ResetTokenLifetime));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await emailSender.SendPasswordResetAsync(user.Email, material.Value, cancellationToken).ConfigureAwait(false);
    }
}
