using Dominodo.Tenants.Contracts.IntegrationEvents;
using Dominodo.Tenants.Domain.Apartments.Events;
using Wolverine;

namespace Dominodo.Tenants.Application.IntegrationBridges;

public sealed class WhenApartmentCreated_PublishIntegrationEvent
{
    public Task Handle(ApartmentCreatedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new ApartmentCreatedIntegrationEvent(message.ApartmentId, message.TenantId)).AsTask();
}
