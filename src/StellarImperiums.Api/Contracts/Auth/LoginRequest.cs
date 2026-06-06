namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// Payload of the login endpoint.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The account password.</param>
public sealed record LoginRequest(string Email, string Password);
