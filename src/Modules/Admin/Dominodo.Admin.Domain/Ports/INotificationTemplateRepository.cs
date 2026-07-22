using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Domain.Ports;

public interface INotificationTemplateRepository
{
    void Add(NotificationTemplate template);

    Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Exact match on (Type, TenantId). A null tenantId targets the global default.
    Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, Guid? tenantId, CancellationToken cancellationToken = default);

    // Lists global defaults plus, when tenantId is set, that tenant's overrides.
    Task<(IReadOnlyList<NotificationTemplate> Items, long TotalCount)> GetAllAsync(Guid? tenantId, PageRequest page, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(NotificationType type, Guid? tenantId, CancellationToken cancellationToken = default);
}
