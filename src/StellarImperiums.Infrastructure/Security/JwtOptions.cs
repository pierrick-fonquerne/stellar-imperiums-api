using System.ComponentModel.DataAnnotations;

namespace StellarImperiums.Infrastructure.Security;

/// <summary>
/// Configuration of the JWT issuance and validation (bound from the <c>Jwt</c> configuration section).
/// </summary>
/// <remarks>
/// The signing key is never stored in <c>appsettings.json</c>: it is provided through
/// user-secrets in development and an environment variable in production. Validation runs
/// at startup (<c>ValidateOnStart</c>) so a missing or weak key prevents the host from booting.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>
    /// Name of the configuration section bound to these options.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets the token issuer.
    /// </summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the token audience.
    /// </summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the access token lifetime in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    /// <summary>
    /// Gets the refresh token lifetime in days.
    /// </summary>
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; init; } = 30;

    /// <summary>
    /// Gets the HMAC-SHA256 signing key (at least 32 characters).
    /// </summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;
}
