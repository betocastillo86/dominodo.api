using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Domain.Ports;

public interface IInAppMessageRepository
{
    void Add(InAppMessage notification);

    Task<InAppMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Self-service read: the caller's own in-app notifications, newest first.
    Task<IReadOnlyList<InAppMessage>> GetForRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    // Admin read: notifications for a tenant (TenantId is a plain column, not ForCurrentTenant-scoped).
    Task<IReadOnlyList<InAppMessage>> GetForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
