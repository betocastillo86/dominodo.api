using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class InAppMessageRepository(AdminDbContext db) : IInAppMessageRepository
{
    public void Add(InAppMessage notification) => db.InAppMessages.Add(notification);

    public Task<InAppMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.InAppMessages.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InAppMessage>> GetForRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var query = db.InAppMessages
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InAppMessage>> GetForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await db.InAppMessages
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
