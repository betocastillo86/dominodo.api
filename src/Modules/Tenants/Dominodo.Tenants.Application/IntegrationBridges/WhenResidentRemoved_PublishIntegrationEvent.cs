using Dominodo.Tenants.Contracts.IntegrationEvents;
using Dominodo.Tenants.Domain.Apartments.Events;
using Wolverine;

namespace Dominodo.Tenants.Application.IntegrationBridges;

public sealed class WhenResidentRemoved_PublishIntegrationEvent
{
    public Task Handle(ResidentRemovedFromApartmentDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new ResidentRemovedFromApartmentIntegrationEvent(
            message.ApartmentId,
            message.TenantId,
            message.ResidentId,
            message.UserId)).AsTask();
}
