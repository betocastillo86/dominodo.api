using Dominodo.Tenants.Contracts.IntegrationEvents;
using Dominodo.Tenants.Domain.Tenants.Events;
using Wolverine;

namespace Dominodo.Tenants.Application.IntegrationBridges;

// Domain→integration bridge: the aggregate's TenantCreatedDomainEvent reaches this in-module Wolverine
// handler via the durable outbox (WolverineUnitOfWork), which then publishes the PUBLIC integration event
// for other modules. Public + method-injected deps (Wolverine's generated code can't touch internals and
// ServiceLocationPolicy blocks constructor injection here) — mirrors the Admin handler style.
public sealed class WhenTenantCreated_PublishIntegrationEvent
{
    public Task Handle(TenantCreatedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new TenantCreatedIntegrationEvent(message.TenantId, message.Slug)).AsTask();
}
