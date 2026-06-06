using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StellarImperiums.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace StellarImperiums.Api.IntegrationTests;

public class StellarApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected virtual int LoginPermitLimit => 1000;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StellarDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Issuer", "StellarImperiums.Tests");
        builder.UseSetting("Jwt:Audience", "StellarImperiums.Tests");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef0123456789abcdef");
        builder.UseSetting("RateLimiting:AuthLogin:PermitLimit", LoginPermitLimit.ToString());
    }

    public HttpClient CreateApiClient(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies
        });
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<StellarApiFactory>;
