using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Domain.Ports;

public interface IInAppMessageRepository
{
    void Add(InAppMessage notification);

    Task<InAppMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Self-service read: the caller's own in-app notifications, newest first.
    Task<(IReadOnlyList<InAppMessage> Items, long TotalCount)> GetForRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken = default);

    // Admin read: notifications for a tenant (TenantId is a plain column, not ForCurrentTenant-scoped).
    Task<(IReadOnlyList<InAppMessage> Items, long TotalCount)> GetForTenantAsync(
        Guid tenantId,
        PageRequest page,
        CancellationToken cancellationToken = default);
}
