using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Infrastructure.Multitenancy;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Operations.Persistence.Repositories;

// Every read is funneled through ForCurrentTenant(tenant) so a caller can never see another tenant's
// deliveries (doc 09).
internal sealed class DeliveryRepository(OperationsDbContext db, ITenantContext tenant) : IDeliveryRepository
{
    public void Add(Delivery delivery) => db.Deliveries.Add(delivery);

    public Task<Delivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Deliveries.ForCurrentTenant(tenant).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Delivery> Items, long TotalCount)> ListAsync(
        PageRequest page,
        DeliveryStatus? status,
        DeliveryType? type,
        Guid? apartmentId,
        CancellationToken cancellationToken = default)
    {
        var query = db.Deliveries.ForCurrentTenant(tenant);

        if (status is not null)
        {
            query = query.Where(d => d.Status == status);
        }

        if (type is not null)
        {
            query = query.Where(d => d.Type == type);
        }

        if (apartmentId is not null)
        {
            query = query.Where(d => d.ApartmentId == apartmentId);
        }

        var ordered = query.OrderByDescending(d => d.ReceivedAtUtc);

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
