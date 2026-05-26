namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// HTTP payload for the user registration endpoint.
/// </summary>
/// <param name="Username">The desired username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The plaintext password (transmitted over HTTPS, never logged).</param>
public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password);
