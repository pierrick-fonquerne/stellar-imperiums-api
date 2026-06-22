namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Command issued to apply a new password using a previously issued reset token.
/// </summary>
/// <param name="Token">The plaintext reset token received by the user via email.</param>
/// <param name="NewPassword">The new plaintext password (never persisted).</param>
public sealed record ResetPasswordCommand(string Token, string NewPassword);
