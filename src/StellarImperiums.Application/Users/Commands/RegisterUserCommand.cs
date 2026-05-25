namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Command issued to register a new player account.
/// </summary>
/// <param name="Username">The player's chosen username (3 to 50 characters).</param>
/// <param name="Email">The player's email address.</param>
/// <param name="PlaintextPassword">The plaintext password (never persisted).</param>
public sealed record RegisterUserCommand(
    string Username,
    string Email,
    string PlaintextPassword);
