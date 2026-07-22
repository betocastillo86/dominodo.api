using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Domain.Ports;

public interface IPushMessageRepository
{
    void Add(PushMessage message);

    // Admin read: push outbox artifacts, optionally filtered by tenant and/or status.
    Task<(IReadOnlyList<PushMessage> Items, long TotalCount)> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        PageRequest page,
        CancellationToken cancellationToken = default);
}
