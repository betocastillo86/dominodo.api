namespace Dominodo.Shared.Abstractions;

// Runtime configuration read port (domain-model §4.4). Injectable from any layer without a Contracts
// dependency. Implemented in the host over an in-memory cache backed by the Admin facade, with
// event-driven invalidation. Resolution: the tenant override if present, otherwise the global value.
public interface ISystemSettings
{
    // Typed read; returns default(T) when the key is absent or the stored value cannot be parsed to T.
    Task<T?> GetAsync<T>(string key, Guid? tenantId = null, CancellationToken cancellationToken = default);

    // Raw carrier (value + declared type) when the caller wants to inspect/parse it explicitly.
    Task<SystemSettingValue?> GetValueAsync(string key, Guid? tenantId = null, CancellationToken cancellationToken = default);
}

// The resolved raw value plus its declared type (e.g. "String", "Int", "Bool", "Json").
public sealed record SystemSettingValue(string Value, string ValueType);
