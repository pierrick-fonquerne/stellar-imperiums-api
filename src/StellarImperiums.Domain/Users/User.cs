using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Users;

/// <summary>
/// Root entity representing a registered user (player or administrator).
/// </summary>
/// <remarks>
/// State transitions (suspension, password change, password reset) are encapsulated in dedicated methods
/// that enforce the domain invariants. The parameterless constructor exists solely for Entity Framework
/// Core materialization and should not be used by application code.
/// </remarks>
public sealed class User : Entity
{
    /// <summary>
    /// Gets the unique username (display name) of the user.
    /// </summary>
    public Username Username { get; private set; }

    /// <summary>
    /// Gets the unique email address of the user.
    /// </summary>
    public Email Email { get; private set; }

    /// <summary>
    /// Gets the current password hash (PHC argon2id format).
    /// </summary>
    public PasswordHash PasswordHash { get; private set; }

    /// <summary>
    /// Gets the functional role assigned to the user.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user is currently suspended.
    /// </summary>
    public bool IsSuspended { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user must change their password on next login.
    /// </summary>
    public bool MustChangePassword { get; private set; }

    /// <summary>
    /// Gets the registration timestamp (UTC).
    /// </summary>
    public DateTimeOffset RegistrationDate { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last successful login, or <c>null</c> if the user never logged in.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets the current password reset token, or <c>null</c> if no reset is in progress.
    /// </summary>
    public string? PasswordResetToken { get; private set; }

    /// <summary>
    /// Gets the expiry timestamp of the current password reset token (UTC), or <c>null</c> if no reset is in progress.
    /// </summary>
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; private set; }

    private User()
    {
        Username = null!;
        Email = null!;
        PasswordHash = null!;
    }

    /// <summary>
    /// Creates a new <see cref="User"/> in the active state.
    /// </summary>
    /// <param name="username">The user's username.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordHash">The user's password hash.</param>
    /// <param name="role">The functional role (defaults to <see cref="UserRole.Player"/>).</param>
    /// <param name="mustChangePassword">Whether the user must change their password on first login.</param>
    /// <param name="registrationDate">An explicit registration timestamp, useful for tests or seeding (defaults to <see cref="DateTimeOffset.UtcNow"/>).</param>
    /// <returns>A new <see cref="User"/> ready to be persisted.</returns>
    public static User Create(
        Username username,
        Email email,
        PasswordHash passwordHash,
        UserRole role = UserRole.Player,
        bool mustChangePassword = false,
        DateTimeOffset? registrationDate = null)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);

        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsSuspended = false,
            MustChangePassword = mustChangePassword,
            RegistrationDate = registrationDate ?? DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Replaces the current password hash and clears any pending reset token.
    /// </summary>
    /// <param name="newHash">The new password hash.</param>
    public void ChangePassword(PasswordHash newHash)
    {
        ArgumentNullException.ThrowIfNull(newHash);

        PasswordHash = newHash;
        MustChangePassword = false;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
    }

    /// <summary>
    /// Suspends the user, blocking subsequent logins.
    /// </summary>
    /// <exception cref="UserAlreadySuspendedException">Thrown when the user is already suspended.</exception>
    public void Suspend()
    {
        if (IsSuspended)
        {
            throw new UserAlreadySuspendedException();
        }

        IsSuspended = true;
    }

    /// <summary>
    /// Reactivates a suspended user.
    /// </summary>
    /// <exception cref="UserNotSuspendedException">Thrown when the user is not currently suspended.</exception>
    public void Reactivate()
    {
        if (!IsSuspended)
        {
            throw new UserNotSuspendedException();
        }

        IsSuspended = false;
    }

    /// <summary>
    /// Records a successful login timestamp.
    /// </summary>
    /// <param name="when">The login timestamp (UTC).</param>
    public void RecordLogin(DateTimeOffset when)
    {
        LastLoginAt = when;
    }

    /// <summary>
    /// Stores a password reset token along with its expiry.
    /// </summary>
    /// <param name="token">The opaque reset token.</param>
    /// <param name="expiresAt">The expiry timestamp (must be in the future).</param>
    /// <exception cref="InvalidPasswordResetException">
    /// Thrown when the token is empty or the expiry is not strictly in the future.
    /// </exception>
    public void StartPasswordReset(string token, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidPasswordResetException("Reset token cannot be empty.");
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidPasswordResetException("Reset token expiry must be in the future.");
        }

        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
    }

    /// <summary>
    /// Applies the new password after verifying that an active, non-expired reset token exists.
    /// </summary>
    /// <param name="newHash">The new password hash to apply.</param>
    /// <param name="now">The current timestamp used to evaluate token expiry (UTC).</param>
    /// <exception cref="InvalidPasswordResetException">
    /// Thrown when no active reset token is present or the token has expired.
    /// </exception>
    public void CompletePasswordReset(PasswordHash newHash, DateTimeOffset now)
    {
        if (PasswordResetToken is null || PasswordResetTokenExpiresAt is null || PasswordResetTokenExpiresAt <= now)
        {
            throw new InvalidPasswordResetException("No active or valid password reset token found.");
        }

        ChangePassword(newHash);
    }
}
