namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Result returned after a successful registration.
/// </summary>
/// <param name="Id">The persisted user identifier.</param>
/// <param name="Username">The normalized username.</param>
/// <param name="Email">The normalized email.</param>
/// <param name="RegistrationDate">The registration timestamp (UTC).</param>
public sealed record RegisterUserResponse(
    int Id,
    string Username,
    string Email,
    DateTimeOffset RegistrationDate);
