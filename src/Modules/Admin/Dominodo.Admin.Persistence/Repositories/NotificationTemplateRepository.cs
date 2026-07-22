using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class NotificationTemplateRepository(AdminDbContext db) : INotificationTemplateRepository
{
    public void Add(NotificationTemplate template) => db.NotificationTemplates.Add(template);

    public Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, Guid? tenantId, CancellationToken cancellationToken = default) =>
        db.NotificationTemplates.FirstOrDefaultAsync(t => t.Type == type && t.TenantId == tenantId, cancellationToken);

    public async Task<(IReadOnlyList<NotificationTemplate> Items, long TotalCount)> GetAllAsync(Guid? tenantId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = db.NotificationTemplates.AsNoTracking();

        query = tenantId is not null
            ? query.Where(t => t.TenantId == null || t.TenantId == tenantId)
            : query.Where(t => t.TenantId == null);

        query = query.OrderBy(t => t.Type);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> ExistsAsync(NotificationType type, Guid? tenantId, CancellationToken cancellationToken = default) =>
        db.NotificationTemplates.AnyAsync(t => t.Type == type && t.TenantId == tenantId, cancellationToken);
}
