using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Infrastructure.Persistence;
using StellarImperiums.Infrastructure.Persistence.Repositories;
using StellarImperiums.Infrastructure.Security;

namespace StellarImperiums.Infrastructure;

/// <summary>
/// Wires Infrastructure services (EF Core, repositories, security) into the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services required by the Application and Api layers.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>ConnectionStrings:Postgres</c> entry is missing.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres is not configured. Set it via user-secrets, environment variable, or appsettings.");

        services.AddDbContext<StellarDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
