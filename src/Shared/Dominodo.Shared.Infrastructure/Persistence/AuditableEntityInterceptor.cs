using Dominodo.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dominodo.Shared.Infrastructure.Persistence;

public sealed class AuditableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateTimestamps(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateTimestamps(DbContext context)
    {
        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Properties.Any(p => p.Metadata.Name == "CreatedAtUtc"))
            {
                entry.Property("CreatedAtUtc").CurrentValue = now;
                entry.Property("UpdatedAtUtc").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified && entry.Properties.Any(p => p.Metadata.Name == "UpdatedAtUtc"))
            {
                entry.Property("UpdatedAtUtc").CurrentValue = now;
            }
        }
    }
}
