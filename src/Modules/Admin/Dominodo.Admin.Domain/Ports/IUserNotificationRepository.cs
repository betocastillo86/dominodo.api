using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Domain.Ports;

public interface IUserNotificationRepository
{
    void Add(UserNotification notification);

    Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Self-service read: the caller's own in-app notifications, newest first.
    Task<IReadOnlyList<UserNotification>> GetForRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    // Admin read: notifications for a tenant (TenantId is a plain column, not ForCurrentTenant-scoped).
    Task<IReadOnlyList<UserNotification>> GetForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
