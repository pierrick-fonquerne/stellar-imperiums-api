namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// HTTP payload for the forgot-password endpoint.
/// </summary>
/// <param name="Email">The email address of the account requesting a password reset.</param>
public sealed record ForgotPasswordRequest(string Email);
