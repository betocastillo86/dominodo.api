using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Requests.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenRequestOpened_PublishIntegrationEvent
{
    public Task Handle(RequestOpenedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new RequestOpenedIntegrationEvent(
            message.RequestId,
            message.TenantId,
            message.Code,
            message.CreatedByUserId,
            message.AssignedToUserId)).AsTask();
}
