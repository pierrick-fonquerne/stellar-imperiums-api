using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Security;

/// <summary>
/// HS256 implementation of <see cref="ITokenService"/> based on <see cref="JsonWebTokenHandler"/>.
/// </summary>
/// <remarks>
/// Access tokens carry only the subject id, username, role, and a unique token id: the email
/// address is deliberately excluded because a JWT is signed but not encrypted. Refresh tokens
/// are 32 random bytes encoded as Base64Url and persisted as an uppercase hexadecimal SHA-256 hash.
/// </remarks>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private const int RefreshTokenByteLength = 32;

    /// <inheritdoc />
    public AccessTokenResult CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwtOptions = options.Value;
        var now = DateTimeOffset.UtcNow;
        var lifetime = TimeSpan.FromMinutes(jwtOptions.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(lifetime).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.UniqueName] = user.Username.Value,
                ["role"] = user.Role.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessTokenResult(token, (int)lifetime.TotalSeconds);
    }

    /// <inheritdoc />
    public RefreshTokenMaterial GenerateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
        var expiresAt = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenLifetimeDays);
        return new RefreshTokenMaterial(value, HashRefreshToken(value), expiresAt);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string refreshTokenValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshTokenValue);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenValue)));
    }
}
