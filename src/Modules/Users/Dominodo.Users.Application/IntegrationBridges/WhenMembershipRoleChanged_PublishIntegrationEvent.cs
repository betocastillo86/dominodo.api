using Dominodo.Users.Contracts.IntegrationEvents;
using Dominodo.Users.Domain.Memberships.Events;
using Wolverine;

namespace Dominodo.Users.Application.IntegrationBridges;

// Domain→integration bridge for role change / reactivation (§1.9). Both raise MembershipRoleChangedDomainEvent
// so the host can evict the permission-cache entry (the effective permission set changed).
public sealed class WhenMembershipRoleChanged_PublishIntegrationEvent
{
    public Task Handle(MembershipRoleChangedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new MembershipChangedIntegrationEvent(
            message.MembershipId, message.UserId, message.TenantId, message.RoleId)).AsTask();
}
