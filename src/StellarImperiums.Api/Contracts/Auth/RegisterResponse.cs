namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// HTTP response returned after a successful registration.
/// </summary>
/// <param name="Id">The newly created user identifier.</param>
/// <param name="Username">The normalized username.</param>
/// <param name="Email">The normalized email.</param>
/// <param name="RegistrationDate">The registration timestamp (UTC).</param>
public sealed record RegisterResponse(
    int Id,
    string Username,
    string Email,
    DateTimeOffset RegistrationDate);
