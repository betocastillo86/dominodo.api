using Dominodo.Shared.Abstractions;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.Tenants.Application;

// Implements the slug → TenantId port (Shared.Abstractions) that the tenant-resolution middleware calls
// per request (doc 09). Registered as a SINGLETON overriding NullTenantDirectory, so it must reach the
// scoped ITenantRepository via IServiceScopeFactory. slug→Id is near-static, so results are cached with a
// bounded TTL; suspended tenants resolve to null (middleware → 400) and are not cached. Cache invalidation
// on rename/suspend arrives in Phase 6 — first cut is TTL-only.
internal sealed class TenantDirectory(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache) : ITenantDirectory
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<Guid?> ResolveSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var cacheKey = CacheKey(slug);
        if (cache.TryGetValue<Guid>(cacheKey, out var cachedId))
        {
            return cachedId;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();

        var tenant = await tenants.GetBySlugAsync(slug, cancellationToken);

        // Only Active/Onboarding tenants resolve; a suspended (or unknown) tenant returns null so the
        // middleware rejects the request. Don't cache a null — status can flip back to active.
        if (tenant is null || tenant.Status == TenantStatus.Suspended)
        {
            return null;
        }

        cache.Set(cacheKey, tenant.Id, CacheTtl);
        return tenant.Id;
    }

    private static string CacheKey(string slug) => TenantDirectoryCache.SlugKey(slug);
}

// Shared cache-key convention so the (public) invalidation handler and the (internal) directory agree on
// the key without exposing the directory's internals to Wolverine's generated code.
public static class TenantDirectoryCache
{
    public static string SlugKey(string slug) => $"tenant-directory:slug:{slug}";
}
