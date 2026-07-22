using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Deliveries.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenDeliveryDelivered_PublishIntegrationEvent
{
    public Task Handle(DeliveryDeliveredDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new DeliveryDeliveredIntegrationEvent(
            message.DeliveryId,
            message.TenantId,
            message.ApartmentId,
            message.DeliveredAtUtc,
            message.DeliveredToUserId)).AsTask();
}
