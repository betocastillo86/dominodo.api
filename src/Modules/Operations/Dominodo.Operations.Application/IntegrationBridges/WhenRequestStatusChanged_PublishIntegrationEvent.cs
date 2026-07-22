using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Requests.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenRequestStatusChanged_PublishIntegrationEvent
{
    public Task Handle(RequestStatusChangedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new RequestStatusChangedIntegrationEvent(
            message.RequestId,
            message.TenantId,
            message.FromStatus.ToString(),
            message.ToStatus.ToString(),
            message.ChangedByUserId)).AsTask();
}
