using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Domain.Ports;

public interface INotificationDeliveryRepository
{
    void Add(NotificationDelivery delivery);
    Task<bool> ExistsForEventAsync(Guid sourceEventId, CancellationToken cancellationToken = default);
}
