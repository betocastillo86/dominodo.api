using Dominodo.Users.Contracts.IntegrationEvents;
using Dominodo.Users.Domain.Memberships.Events;
using Wolverine;

namespace Dominodo.Users.Application.IntegrationBridges;

// Domain→integration bridge: the aggregate's MembershipCreatedDomainEvent reaches this in-module Wolverine
// handler via the durable outbox (WolverineUnitOfWork), which then publishes the PUBLIC integration event.
// Public + method-injected deps (Wolverine's generated code can't touch internals and ServiceLocationPolicy
// blocks constructor injection here) — mirrors the Tenants bridge style.
public sealed class WhenMembershipCreated_PublishIntegrationEvent
{
    public Task Handle(MembershipCreatedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new MembershipCreatedIntegrationEvent(
            message.MembershipId, message.UserId, message.TenantId, message.RoleId)).AsTask();
}
