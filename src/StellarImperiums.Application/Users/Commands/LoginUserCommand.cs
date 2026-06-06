namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Command issued to authenticate a player with email and password.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The plaintext password (never persisted).</param>
public sealed record LoginUserCommand(string Email, string Password);
