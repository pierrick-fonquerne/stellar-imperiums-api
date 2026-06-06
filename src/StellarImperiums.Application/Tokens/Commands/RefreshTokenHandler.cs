using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Tokens.Exceptions;
using StellarImperiums.Application.Users.Exceptions;

namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Wolverine handler that rotates a refresh token and issues a new access token.
/// </summary>
/// <remarks>
/// Presenting a token that was already rotated is treated as a theft signal: the whole
/// rotation family is revoked so both the attacker and the legitimate session are cut off.
/// </remarks>
public sealed class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ITokenService tokenService)
{
    /// <summary>
    /// Handles the refresh command.
    /// </summary>
    /// <param name="command">The refresh command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The new access token and the rotated refresh token material.</returns>
    /// <exception cref="RefreshTokenRejectedException">
    /// Thrown when the token is unknown, expired, revoked, replayed, or its owner no longer exists.
    /// </exception>
    /// <exception cref="UserSuspendedException">Thrown when the owner account is suspended.</exception>
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(command.RefreshTokenValue);
        var token = await refreshTokens.GetByHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (token is null)
        {
            throw new RefreshTokenRejectedException();
        }

        var now = DateTimeOffset.UtcNow;

        if (token.IsRotated)
        {
            if (!token.IsRevoked)
            {
                await refreshTokens.RevokeFamilyAsync(token.FamilyId, now, cancellationToken).ConfigureAwait(false);
                await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            throw new RefreshTokenRejectedException();
        }

        if (token.IsRevoked || token.IsExpired(now))
        {
            throw new RefreshTokenRejectedException();
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new RefreshTokenRejectedException();

        if (user.IsSuspended)
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, now, cancellationToken).ConfigureAwait(false);
            await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new UserSuspendedException();
        }

        var material = tokenService.GenerateRefreshToken();
        var successor = token.Rotate(material.Hash, material.ExpiresAt, now);

        await refreshTokens.AddAsync(successor, cancellationToken).ConfigureAwait(false);
        await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var access = tokenService.CreateAccessToken(user);
        return new RefreshTokenResult(access.Token, access.ExpiresInSeconds, material.Value, material.ExpiresAt);
    }
}
