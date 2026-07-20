using Dominodo.Shared.Abstractions;
using Dominodo.Users.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Dominodo.Api.Auth;

// Host-side implementation of the permission port: it may reach the Users module facade (which
// Shared.Infrastructure cannot). Effective permissions are cached per (userId, tenantId) with a
// short TTL. When a tenant is resolved, the facade returns platform ∪ tenant-membership permissions;
// otherwise only the user's Platform-scope permissions resolve.
// See docs/architecture/12-permission-authorization.md.
internal sealed class CachingPermissionProvider(
    IUsersModuleApi usersModule,
    IMemoryCache cache) : IPermissionProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    // Cache-key builder, shared with the membership-change invalidator so eviction targets the exact key.
    public static string CacheKey(Guid userId, Guid? tenantId) => $"perm:{userId:N}:{tenantId:N}";

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(userId, tenantId);

        if (cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        // With a resolved tenant, the facade returns platform ∪ the user's Active-membership permissions
        // in that tenant (domain-model §1.8). Without one, only Platform-scope permissions resolve.
        var codes = tenantId is not null
            ? await usersModule.GetEffectivePermissionsAsync(userId, tenantId.Value, cancellationToken)
            : await usersModule.GetPlatformPermissionsAsync(userId, cancellationToken);

        var effective = new HashSet<string>(codes.Select(p => p.Code), StringComparer.Ordinal);

        // Freshness beyond the TTL is handled by WhenMembershipChanged_InvalidatePermissionCache, which
        // evicts the exact perm:{userId}:{tenantId} key on membership create/suspend/role-change events.
        var result = (IReadOnlySet<string>)effective;
        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }
}
