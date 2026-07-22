using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class InAppMessageRepository(AdminDbContext db) : IInAppMessageRepository
{
    public void Add(InAppMessage notification) => db.InAppMessages.Add(notification);

    public Task<InAppMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.InAppMessages.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<InAppMessage> Items, long TotalCount)> GetForRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.InAppMessages
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedAtUtc);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<InAppMessage> Items, long TotalCount)> GetForTenantAsync(
        Guid tenantId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.InAppMessages
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAtUtc);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return (items, total);
    }
}
