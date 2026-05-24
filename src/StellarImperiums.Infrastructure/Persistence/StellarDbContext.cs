using Microsoft.EntityFrameworkCore;

namespace StellarImperiums.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the Stellar Imperiums relational store (PostgreSQL).
/// </summary>
/// <remarks>
/// Entity configurations are loaded by convention from any <see cref="IEntityTypeConfiguration{TEntity}"/>
/// implementation discovered in the Infrastructure assembly.
/// </remarks>
public class StellarDbContext(DbContextOptions<StellarDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StellarDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
