using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Wolverine handler that validates a password reset token and applies the new password.
/// </summary>
/// <remarks>
/// On success all refresh tokens belonging to the user are revoked to force re-authentication
/// with the new credentials, preventing session fixation after a password change.
/// Both unknown and expired tokens yield the same <see cref="InvalidPasswordResetTokenException"/>
/// so callers cannot distinguish between the two cases.
/// The lookup-before-hash strategy avoids running the password hasher when the token
/// is not found, mitigating denial-of-service via repeated invalid tokens.
/// </remarks>
public sealed class ResetPasswordHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    /// <summary>
    /// Handles the reset-password command.
    /// </summary>
    /// <param name="command">The reset-password command carrying the token and new password.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="InvalidPasswordResetTokenException">
    /// Thrown when the token is unknown or has expired.
    /// </exception>
    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashPasswordResetToken(command.Token);
        var user = await users.GetByPasswordResetTokenAsync(tokenHash, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new InvalidPasswordResetTokenException();
        }

        var newHash = passwordHasher.Hash(command.NewPassword);
        var now = DateTimeOffset.UtcNow;

        try
        {
            user.CompletePasswordReset(newHash, now);
        }
        catch (InvalidPasswordResetException)
        {
            throw new InvalidPasswordResetTokenException();
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
