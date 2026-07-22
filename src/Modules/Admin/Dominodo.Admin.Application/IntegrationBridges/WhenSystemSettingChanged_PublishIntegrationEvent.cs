using Dominodo.Admin.Contracts.IntegrationEvents;
using Dominodo.Admin.Domain.Configuration.Events;
using Wolverine;

namespace Dominodo.Admin.Application.IntegrationBridges;

// Domain→integration bridge: the aggregate's SystemSettingChangedDomainEvent reaches this in-module
// Wolverine handler via the durable outbox (WolverineUnitOfWork), which then publishes the PUBLIC
// integration event so the host can evict its settings cache. Public + method-injected deps (Wolverine's
// generated code can't touch internals) — mirrors the Tenants bridge style.
public sealed class WhenSystemSettingChanged_PublishIntegrationEvent
{
    public Task Handle(SystemSettingChangedDomainEvent message, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new SystemSettingChangedIntegrationEvent(message.Key, message.TenantId)).AsTask();
}
