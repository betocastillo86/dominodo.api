using Microsoft.Extensions.Caching.Memory;

namespace Dominodo.Tenants.Application.IntegrationBridges;

// Evicts the directory's slug → Id cache entry when a tenant's status changes, so a suspension takes
// effect immediately (a suspended tenant must stop resolving) rather than lingering until the TTL. The
// domain event reaches this in-module Wolverine handler via the durable outbox. Public + method-injected
// IMemoryCache (the same singleton the directory uses); keys are shared via TenantDirectoryCache.
public sealed class WhenTenantStatusChanged_InvalidateDirectoryCache
{
    public void Handle(Dominodo.Tenants.Domain.Tenants.Events.TenantStatusChangedDomainEvent message, IMemoryCache cache) =>
        cache.Remove(TenantDirectoryCache.SlugKey(message.Slug));
}
