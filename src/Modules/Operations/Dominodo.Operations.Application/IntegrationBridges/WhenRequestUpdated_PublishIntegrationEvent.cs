using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Requests.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenRequestUpdated_PublishIntegrationEvent
{
    public Task Handle(RequestUpdatedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new RequestUpdatedIntegrationEvent(
            message.RequestId,
            message.TenantId,
            message.UpdateId,
            message.AuthorUserId)).AsTask();
}
