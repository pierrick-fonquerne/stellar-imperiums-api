using StellarImperiums.Application.Abstractions;

namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Wolverine handler that revokes the refresh token family of a session.
/// </summary>
/// <remarks>
/// Logout is idempotent: a missing or unknown token is silently ignored so the endpoint
/// always succeeds from the client's point of view.
/// </remarks>
public sealed class LogoutHandler(IRefreshTokenRepository refreshTokens, ITokenService tokenService)
{
    /// <summary>
    /// Handles the logout command.
    /// </summary>
    /// <param name="command">The logout command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshTokenValue))
        {
            return;
        }

        var hash = tokenService.HashRefreshToken(command.RefreshTokenValue);
        var token = await refreshTokens.GetByHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (token is null)
        {
            return;
        }

        await refreshTokens.RevokeFamilyAsync(token.FamilyId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
