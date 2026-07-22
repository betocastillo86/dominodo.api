using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Infrastructure.Multitenancy;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Operations.Persistence.Repositories;

// Every read is funneled through ForCurrentTenant(tenant) so a caller can never see another tenant's
// visits (doc 09).
internal sealed class VisitRepository(OperationsDbContext db, ITenantContext tenant) : IVisitRepository
{
    public void Add(Visit visit) => db.Visits.Add(visit);

    public Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Visits.ForCurrentTenant(tenant).FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Visit> Items, long TotalCount)> ListAsync(
        PageRequest page,
        VisitStatus? status,
        VisitType? type,
        Guid? apartmentId,
        CancellationToken cancellationToken = default)
    {
        var query = db.Visits.ForCurrentTenant(tenant);

        if (status is not null)
        {
            query = query.Where(v => v.Status == status);
        }

        if (type is not null)
        {
            query = query.Where(v => v.Type == type);
        }

        if (apartmentId is not null)
        {
            query = query.Where(v => v.ApartmentId == apartmentId);
        }

        var ordered = query.OrderByDescending(v => v.EntryAtUtc);

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
