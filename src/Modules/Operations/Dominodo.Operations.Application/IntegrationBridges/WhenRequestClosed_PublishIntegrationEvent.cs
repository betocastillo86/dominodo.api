using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Requests.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenRequestClosed_PublishIntegrationEvent
{
    public Task Handle(RequestClosedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new RequestClosedIntegrationEvent(
            message.RequestId,
            message.TenantId,
            message.ClosedAtUtc)).AsTask();
}
