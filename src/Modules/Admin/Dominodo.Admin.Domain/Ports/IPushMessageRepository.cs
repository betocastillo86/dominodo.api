using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Domain.Ports;

public interface IPushMessageRepository
{
    void Add(PushMessage message);

    // Admin read: push outbox artifacts, optionally filtered by tenant and/or status.
    Task<IReadOnlyList<PushMessage>> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        CancellationToken cancellationToken = default);
}
