using Dominodo.Shared.Abstractions;
using Dominodo.Users.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Dominodo.Api.Auth;

// Host-side implementation of the permission port: it may reach the Users module facade (which
// Shared.Infrastructure cannot). Effective permissions are cached per (userId, tenantId) with a
// short TTL. The tenant branch is a no-op until the Membership slice lands — today it resolves the
// user's Platform-scope permissions, which already protects every platform-scoped endpoint.
// See docs/architecture/12-permission-authorization.md.
internal sealed class CachingPermissionProvider(
    IUsersModuleApi usersModule,
    IMemoryCache cache) : IPermissionProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"perm:{userId:N}:{tenantId:N}";

        if (cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var platform = await usersModule.GetPlatformPermissionsAsync(userId, cancellationToken);

        var effective = new HashSet<string>(platform.Select(p => p.Code), StringComparer.Ordinal);

        // TODO (Fase 4 — Membership slice, doc 12): when tenantId is present, union the user's
        // tenant-role permissions via IUsersModuleApi.GetEffectivePermissionsAsync(userId, tenantId).
        // Blocked: the Membership aggregate does not exist yet, so only Platform permissions resolve.
        // Once added, `tenantId` here stops being ignored.

        // TODO (Fase 5 — cache invalidation, doc 12): today freshness relies on the short TTL. When
        // Users publishes RolePermissionsChanged / MembershipChanged integration events, subscribe a
        // Wolverine handler that evicts the affected keys. IMemoryCache has no evict-by-prefix, so
        // either track keys per user or move to an IChangeToken-backed entry to invalidate in bulk.
        var result = (IReadOnlySet<string>)effective;
        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }
}
