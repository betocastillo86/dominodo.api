using Dominodo.Admin.Contracts.IntegrationEvents;
using Microsoft.Extensions.Caching.Memory;

namespace Dominodo.Api.Auth;

// Evicts the settings-cache entry when a SystemSetting is created or updated, so ISystemSettings reads
// reflect the change immediately instead of lingering until the CachingSystemSettings TTL. The event
// carries Key + TenantId, so we evict the exact setting:{key}:{tenantId} key. Public + method-injected
// IMemoryCache (the same singleton the settings port uses) — mirrors the permission-cache invalidator.
public sealed class WhenSystemSettingChanged_InvalidateSettingsCache
{
    public void Handle(SystemSettingChangedIntegrationEvent message, IMemoryCache cache) =>
        cache.Remove(CachingSystemSettings.CacheKey(message.Key, message.TenantId));
}
