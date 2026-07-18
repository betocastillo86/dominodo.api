using Dominodo.Tenants.Contracts.IntegrationEvents;
using Dominodo.Tenants.Domain.Apartments.Events;
using Wolverine;

namespace Dominodo.Tenants.Application.IntegrationBridges;

public sealed class WhenResidentAssigned_PublishIntegrationEvent
{
    public Task Handle(ResidentAssignedToApartmentDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new ResidentAssignedToApartmentIntegrationEvent(
            message.ApartmentId,
            message.TenantId,
            message.ResidentId,
            message.UserId)).AsTask();
}
