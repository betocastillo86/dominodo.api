using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class EmailMessageRepository(AdminDbContext db) : IEmailMessageRepository
{
    public void Add(EmailMessage message) => db.EmailMessages.Add(message);

    public async Task<IReadOnlyList<EmailMessage>> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = db.EmailMessages.AsNoTracking();

        if (tenantId is not null)
        {
            query = query.Where(m => m.TenantId == tenantId);
        }

        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }

        return await query.OrderByDescending(m => m.ScheduledAtUtc).ToListAsync(cancellationToken);
    }
}
