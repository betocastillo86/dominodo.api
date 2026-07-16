using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class NotificationDeliveryRepository(AdminDbContext db) : INotificationDeliveryRepository
{
    public void Add(NotificationDelivery delivery) => db.NotificationDeliveries.Add(delivery);

    public Task<bool> ExistsForEventAsync(Guid sourceEventId, CancellationToken cancellationToken = default) =>
        db.NotificationDeliveries.AnyAsync(d => d.SourceEventId == sourceEventId, cancellationToken);
}
