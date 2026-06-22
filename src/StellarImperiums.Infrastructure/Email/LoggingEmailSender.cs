using Microsoft.Extensions.Logging;
using StellarImperiums.Application.Abstractions;
using DomainEmail = StellarImperiums.Domain.Users.Email;

namespace StellarImperiums.Infrastructure.Email;

/// <summary>
/// Development implementation of <see cref="IEmailSender"/> that writes outgoing emails to the application log.
/// </summary>
/// <remarks>
/// This sender is intended for local development and integration tests only.
/// Replace with a real SMTP or transactional email provider for production deployments.
/// </remarks>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    /// <remarks>
    /// This is a development implementation that logs the reset token instead of sending a real email.
    /// MUST NOT be used in production: logging the token value exposes a security-sensitive secret.
    /// </remarks>
    public Task SendPasswordResetAsync(DomainEmail recipient, string resetToken, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Password reset requested for {Recipient}. Token: {ResetToken}",
            recipient.Value,
            resetToken);
        return Task.CompletedTask;
    }
}
