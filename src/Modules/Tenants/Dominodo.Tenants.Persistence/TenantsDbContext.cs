using Dominodo.Shared.Kernel;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Tenants.Persistence;

internal sealed class TenantsDbContext(DbContextOptions<TenantsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "tenants";

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Apartment> Apartments => Set<Apartment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantsDbContext).Assembly);

        // Wolverine's durable message storage (envelope tables) is provisioned by the bus in the
        // "wolverine" schema, not by this module's EF migrations (doc 07).
    }
}
