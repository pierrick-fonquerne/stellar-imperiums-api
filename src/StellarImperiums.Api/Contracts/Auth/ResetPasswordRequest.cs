namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// HTTP payload for the reset-password endpoint.
/// </summary>
/// <param name="Token">The plaintext reset token received by the user via email.</param>
/// <param name="NewPassword">The new plaintext password (transmitted over HTTPS, never logged).</param>
public sealed record ResetPasswordRequest(string Token, string NewPassword);
