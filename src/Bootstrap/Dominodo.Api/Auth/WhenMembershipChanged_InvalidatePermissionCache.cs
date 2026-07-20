using Dominodo.Users.Contracts.IntegrationEvents;
using Microsoft.Extensions.Caching.Memory;

namespace Dominodo.Api.Auth;

// Evicts the caller's permission-cache entry when their membership changes, so a suspend / role-change /
// (re)invite takes effect within ~1-2s instead of lingering until the CachingPermissionProvider TTL.
// IMemoryCache has no evict-by-prefix, but each integration event carries UserId + TenantId, so we evict
// the exact perm:{userId}:{tenantId} key. Public + method-injected IMemoryCache (the same singleton the
// permission provider uses) — mirrors the Tenants directory-cache invalidator style.
public sealed class WhenMembershipChanged_InvalidatePermissionCache
{
    public void Handle(MembershipCreatedIntegrationEvent message, IMemoryCache cache) =>
        cache.Remove(CachingPermissionProvider.CacheKey(message.UserId, message.TenantId));

    public void Handle(MembershipSuspendedIntegrationEvent message, IMemoryCache cache) =>
        cache.Remove(CachingPermissionProvider.CacheKey(message.UserId, message.TenantId));

    public void Handle(MembershipChangedIntegrationEvent message, IMemoryCache cache) =>
        cache.Remove(CachingPermissionProvider.CacheKey(message.UserId, message.TenantId));
}
