using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StellarImperiums.Api.Middleware;
using StellarImperiums.Application.Users.Commands;
using StellarImperiums.Infrastructure;
using Wolverine;
using Wolverine.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Postgres is not configured. Set it via user-secrets, environment variable, or appsettings.");

builder.Services.AddInfrastructure(postgresConnectionString);

builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(RegisterUserValidator).Assembly);
    opts.ServiceLocationPolicy = JasperFx.CodeGeneration.Model.ServiceLocationPolicy.AlwaysAllowed;
    opts.UseFluentValidation();
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnectionString, name: "postgres", tags: ["db"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<DomainExceptionMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(payload);
    }
});

app.Run();

/// <summary>
/// Exposed for integration tests via <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public partial class Program;
