using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Domain.Ports;

public interface IEmailMessageRepository
{
    void Add(EmailMessage message);

    // Admin read: email outbox artifacts, optionally filtered by tenant and/or status.
    Task<(IReadOnlyList<EmailMessage> Items, long TotalCount)> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        PageRequest page,
        CancellationToken cancellationToken = default);
}
