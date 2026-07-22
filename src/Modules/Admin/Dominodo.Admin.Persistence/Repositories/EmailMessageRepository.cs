using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class EmailMessageRepository(AdminDbContext db) : IEmailMessageRepository
{
    public void Add(EmailMessage message) => db.EmailMessages.Add(message);

    public async Task<(IReadOnlyList<EmailMessage> Items, long TotalCount)> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        PageRequest page,
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

        var ordered = query.OrderByDescending(m => m.ScheduledAtUtc);

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return (items, total);
    }
}
