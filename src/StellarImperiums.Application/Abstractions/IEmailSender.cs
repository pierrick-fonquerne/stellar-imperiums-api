using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Sends transactional emails to users on behalf of the application.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a password reset email containing the one-time reset token.
    /// </summary>
    /// <param name="recipient">The email address to send the message to.</param>
    /// <param name="resetToken">The plaintext reset token (never persisted; valid for a limited time).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SendPasswordResetAsync(Email recipient, string resetToken, CancellationToken cancellationToken = default);
}
