using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Visits.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenVisitRegistered_PublishIntegrationEvent
{
    public Task Handle(VisitRegisteredDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new VisitRegisteredIntegrationEvent(
            message.VisitId,
            message.TenantId,
            message.ApartmentId,
            message.RegisteredByUserId)).AsTask();
}
