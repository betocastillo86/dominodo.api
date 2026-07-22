using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class PushMessageRepository(AdminDbContext db) : IPushMessageRepository
{
    public void Add(PushMessage message) => db.PushMessages.Add(message);

    public async Task<IReadOnlyList<PushMessage>> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = db.PushMessages.AsNoTracking();

        if (tenantId is not null)
        {
            query = query.Where(m => m.TenantId == tenantId);
        }

        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }

        return await query.OrderByDescending(m => m.SentAtUtc).ToListAsync(cancellationToken);
    }
}
