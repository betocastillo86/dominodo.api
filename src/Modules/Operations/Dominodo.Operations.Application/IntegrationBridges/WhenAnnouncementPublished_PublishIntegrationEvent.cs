using Dominodo.Operations.Contracts.IntegrationEvents;
using Dominodo.Operations.Domain.Announcements.Events;
using Wolverine;

namespace Dominodo.Operations.Application.IntegrationBridges;

public sealed class WhenAnnouncementPublished_PublishIntegrationEvent
{
    public Task Handle(AnnouncementPublishedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new AnnouncementPublishedIntegrationEvent(
            message.AnnouncementId,
            message.TenantId,
            message.PublishedByUserId,
            message.PublishedAtUtc)).AsTask();
}
