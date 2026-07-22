using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Domain.Ports;

// All reads are implicitly scoped to the current tenant by the implementation (ForCurrentTenant, doc 09).
public interface IDeliveryRepository
{
    void Add(Delivery delivery);
    Task<Delivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Delivery> Items, long TotalCount)> ListAsync(
        PageRequest page,
        DeliveryStatus? status,
        DeliveryType? type,
        Guid? apartmentId,
        CancellationToken cancellationToken = default);
}
