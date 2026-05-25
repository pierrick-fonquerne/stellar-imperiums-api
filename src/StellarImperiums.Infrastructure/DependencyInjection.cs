using Microsoft.EntityFrameworkCore;
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
    /// <param name="postgresConnectionString">The PostgreSQL connection string.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string postgresConnectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(postgresConnectionString);

        services.AddDbContext<StellarDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        return services;
    }
}
