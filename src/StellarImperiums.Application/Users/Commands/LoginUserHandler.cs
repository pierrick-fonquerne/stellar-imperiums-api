using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;
using DomainEmail = StellarImperiums.Domain.Users.Email;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Wolverine handler that authenticates a user and issues an access token plus a refresh token.
/// </summary>
/// <remarks>
/// When the email is unknown, the password is still verified against a constant dummy hash so the
/// response time does not reveal whether the account exists (timing attack mitigation).
/// </remarks>
public sealed class LoginUserHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    private static readonly PasswordHash DummyHash = PasswordHash.Create(
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY");

    /// <summary>
    /// Handles the login command.
    /// </summary>
    /// <param name="command">The login command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The issued tokens and the authenticated user's profile summary.</returns>
    /// <exception cref="InvalidCredentialsException">Thrown when the email or password is wrong.</exception>
    /// <exception cref="UserSuspendedException">Thrown when the account is suspended.</exception>
    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var email = DomainEmail.Create(command.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            passwordHasher.Verify(command.Password, DummyHash);
            throw new InvalidCredentialsException();
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (user.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin(now);

        var access = tokenService.CreateAccessToken(user);
        var material = tokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(user.Id, material.Hash, Guid.NewGuid(), material.ExpiresAt, now);

        await refreshTokens.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LoginUserResult(
            access.Token,
            access.ExpiresInSeconds,
            user.Id,
            user.Username.Value,
            user.Email.Value,
            user.Role.ToString(),
            user.MustChangePassword,
            material.Value,
            material.ExpiresAt);
    }
}
