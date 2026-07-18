using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Tenants.Persistence.Repositories;

internal sealed class TenantRepository(TenantsDbContext db) : ITenantRepository
{
    public void Add(Tenant tenant) => db.Tenants.Add(tenant);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByIdWithFeaturesAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Tenants.Include(t => t.Features).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);

    public async Task<(IReadOnlyList<Tenant> Items, long TotalCount)> ListAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Tenants.OrderBy(t => t.Name);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
