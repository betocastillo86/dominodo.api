using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Domain.Ports;

public interface INotificationTemplateRepository
{
    void Add(NotificationTemplate template);

    Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Exact match on (Type, TenantId). A null tenantId targets the global default.
    Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, Guid? tenantId, CancellationToken cancellationToken = default);

    // Lists global defaults plus, when tenantId is set, that tenant's overrides.
    Task<IReadOnlyList<NotificationTemplate>> GetAllAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(NotificationType type, Guid? tenantId, CancellationToken cancellationToken = default);
}
