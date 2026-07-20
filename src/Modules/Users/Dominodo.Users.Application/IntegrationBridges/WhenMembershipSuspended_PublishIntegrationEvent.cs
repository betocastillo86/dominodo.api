using Dominodo.Users.Contracts.IntegrationEvents;
using Dominodo.Users.Domain.Memberships.Events;
using Wolverine;

namespace Dominodo.Users.Application.IntegrationBridges;

// Domain→integration bridge for suspension (§1.9). Publishes the public integration event that the host
// consumes to evict the user's permission-cache entry immediately.
public sealed class WhenMembershipSuspended_PublishIntegrationEvent
{
    public Task Handle(MembershipSuspendedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new MembershipSuspendedIntegrationEvent(
            message.MembershipId, message.UserId, message.TenantId)).AsTask();
}
