using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Deliveries.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenDeliveryRegistered_PublishIntegrationEvent
{
    public Task Handle(DeliveryRegisteredDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new DeliveryRegisteredIntegrationEvent(
            message.DeliveryId,
            message.TenantId,
            message.Code,
            message.ApartmentId,
            message.RegisteredByUserId)).AsTask();
}
