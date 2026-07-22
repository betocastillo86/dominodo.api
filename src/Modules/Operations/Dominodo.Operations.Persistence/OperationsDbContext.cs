using Dominodo.Operations.Domain.Announcements;
using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Operations.Persistence;

internal sealed class OperationsDbContext(DbContextOptions<OperationsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "operations";

    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationsDbContext).Assembly);
    }
}
