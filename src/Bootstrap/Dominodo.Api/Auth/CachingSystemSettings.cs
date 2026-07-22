using System.Globalization;
using System.Text.Json;
using Dominodo.Admin.Contracts;
using Dominodo.Shared.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Dominodo.Api.Auth;

// Host-side implementation of the runtime config port (domain-model §4.4): it reaches the Admin module
// facade (which Shared.Infrastructure/Abstractions cannot) and caches the resolved value per (key,
// tenantId) with a short TTL. Mirrors CachingPermissionProvider. Exact eviction on write is handled by
// WhenSystemSettingChanged_InvalidateSettingsCache; the TTL is the backstop for the case where a global
// value changes and per-tenant entries that fell through to it must refresh.
internal sealed class CachingSystemSettings(
    IAdminModuleApi adminModule,
    IMemoryCache cache) : ISystemSettings
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    // Cache-key builder, shared with the invalidator so eviction targets the exact key.
    public static string CacheKey(string key, Guid? tenantId) => $"setting:{key}:{tenantId:N}";

    public async Task<SystemSettingValue?> GetValueAsync(
        string key,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(key, tenantId);

        if (cache.TryGetValue(cacheKey, out SystemSettingValue? cached))
        {
            return cached;
        }

        var dto = await adminModule.GetSettingAsync(key, tenantId, cancellationToken);
        var value = dto is null ? null : new SystemSettingValue(dto.Value, dto.ValueType);

        // Cache negatives too (null) so a missing key doesn't hit the facade every request.
        cache.Set(cacheKey, value, CacheTtl);
        return value;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(key, tenantId, cancellationToken);
        if (value is null)
        {
            return default;
        }

        return Parse<T>(value.Value);
    }

    private static T? Parse<T>(string raw)
    {
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
            if (target == typeof(string))
            {
                return (T)(object)raw;
            }

            if (target == typeof(bool))
            {
                return (T)(object)bool.Parse(raw);
            }

            if (target == typeof(int))
            {
                return (T)(object)int.Parse(raw, CultureInfo.InvariantCulture);
            }

            if (target == typeof(long))
            {
                return (T)(object)long.Parse(raw, CultureInfo.InvariantCulture);
            }

            // Fall back to JSON for complex/object types.
            return JsonSerializer.Deserialize<T>(raw);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidCastException)
        {
            return default;
        }
    }
}
