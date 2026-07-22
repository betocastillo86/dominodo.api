using Dominodo.Admin.Application.Configuration.SeedTenantDefaults;
using Dominodo.Tenants.Contracts.IntegrationEvents;
using MediatR;

namespace Dominodo.Admin.Application.Consumers;

// Inbound adapter (public, like a controller): when a conjunto is created, seed its default Admin config
// and templates (domain-model §5.3). Consumes the PUBLIC Tenants integration event and dispatches THIS
// module's own internal command — the boundary holds. Method-injected deps (Wolverine's sanctioned
// pattern). The command is idempotent, so at-least-once redelivery is safe (doc 11) — mirrors
// UserOtpRequestedHandler.
public sealed class TenantCreatedConsumer
{
    public Task Handle(TenantCreatedIntegrationEvent message, ISender sender, CancellationToken ct) =>
        sender.Send(new SeedTenantDefaultsCommand(message.TenantId), ct);
}
