namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Command issued to initiate a password reset flow for the given email address.
/// </summary>
/// <param name="Email">The email address of the account requesting a reset.</param>
public sealed record ForgotPasswordCommand(string Email);
