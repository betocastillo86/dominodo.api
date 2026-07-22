using Dominodo.Shared.Infrastructure.Multitenancy;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Tenants.Persistence.Repositories;

// Every read is funneled through ForCurrentTenant(tenantContext) so a caller can never see another
// tenant's apartments (doc 09). Application handlers stay free of Shared.Infrastructure — the scoping
// lives here, in the persistence adapter, where ForCurrentTenant is allowed.
internal sealed class ApartmentRepository(TenantsDbContext db, ITenantContext tenant) : IApartmentRepository
{
    public void Add(Apartment apartment) => db.Apartments.Add(apartment);

    public Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Apartments.ForCurrentTenant(tenant).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Apartment?> GetByIdWithResidentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Apartments
            .ForCurrentTenant(tenant)
            .Include(a => a.Residents)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsByTowerAndNumberAsync(string? tower, string number, CancellationToken cancellationToken = default) =>
        db.Apartments
            .ForCurrentTenant(tenant)
            .AnyAsync(a => a.Tower == tower && a.Number == number, cancellationToken);

    // Explicit-tenant check for the facade: the caller supplies the TenantId, so we match both id and
    // tenant directly (no context scoping) — still cross-tenant-safe since both must match.
    public Task<bool> ExistsForTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default) =>
        db.Apartments.AnyAsync(a => a.Id == id && a.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<Apartment>> ListForResidentAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.Apartments
            .ForCurrentTenant(tenant)
            .Where(a => a.Residents.Any(r => r.UserId == userId && r.IsActive))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Apartment> Items, long TotalCount)> ListAsync(
        PageRequest page,
        string? tower,
        ApartmentType? type,
        ApartmentStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = db.Apartments.ForCurrentTenant(tenant);

        if (!string.IsNullOrWhiteSpace(tower))
        {
            query = query.Where(a => a.Tower == tower);
        }

        if (type is not null)
        {
            query = query.Where(a => a.Type == type);
        }

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var ordered = query.OrderBy(a => a.Tower).ThenBy(a => a.Number);

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
