using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;
using DomainEmail = StellarImperiums.Domain.Users.Email;
using DomainUser = StellarImperiums.Domain.Users.User;
using DomainUsername = StellarImperiums.Domain.Users.Username;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Wolverine handler that turns a <see cref="RegisterUserCommand"/> into a persisted
/// <see cref="DomainUser"/> aggregate and returns the resulting <see cref="RegisterUserResponse"/>.
/// </summary>
public sealed class RegisterUserHandler(IUserRepository repository, IPasswordHasher passwordHasher)
{
    /// <summary>
    /// Handles the registration command.
    /// </summary>
    /// <param name="command">The registration command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The newly registered user projected to a response DTO.</returns>
    /// <exception cref="UsernameAlreadyTakenException">Thrown when the username is already taken.</exception>
    /// <exception cref="EmailAlreadyTakenException">Thrown when the email is already taken.</exception>
    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var username = DomainUsername.Create(command.Username);
        var email = DomainEmail.Create(command.Email);

        if (await repository.UsernameExistsAsync(username, cancellationToken).ConfigureAwait(false))
        {
            throw new UsernameAlreadyTakenException(username.Value);
        }

        if (await repository.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
        {
            throw new EmailAlreadyTakenException(email.Value);
        }

        var passwordHash = passwordHasher.Hash(command.PlaintextPassword);
        var user = DomainUser.Create(username, email, passwordHash);

        await repository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RegisterUserResponse(
            user.Id,
            user.Username.Value,
            user.Email.Value,
            user.RegistrationDate);
    }
}
