namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated response body for the system-settings endpoints. Mirrors the API's
/// <c>SystemSettingDto</c> by value. <c>TenantId</c> null = the global value; set = a tenant override.
/// <c>ValueType</c> is the <c>SystemSettingValueType</c> enum serialized as its name (e.g. "String").
/// </summary>
public sealed record SystemSettingModel
{
    public string? Key { get; init; }
    public Guid? TenantId { get; init; }
    public string? Value { get; init; }
    public string? ValueType { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
