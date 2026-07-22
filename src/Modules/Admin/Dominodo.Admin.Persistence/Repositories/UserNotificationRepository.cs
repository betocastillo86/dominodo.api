using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class UserNotificationRepository(AdminDbContext db) : IUserNotificationRepository
{
    public void Add(UserNotification notification) => db.UserNotifications.Add(notification);

    public Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserNotification>> GetForRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var query = db.UserNotifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserNotification>> GetForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await db.UserNotifications
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
