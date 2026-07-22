namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/system-settings/{key}</c>. Mirrors the API's
/// <c>UpdateSystemSettingRequest</c> by value (the key travels in the route). <c>Value</c> is required
/// (non-null); <c>ValueType</c> must parse to the <c>SystemSettingValueType</c> enum. Any field is
/// overridable for the 400 cases via <c>model with { ... }</c>.
/// </summary>
public sealed record UpdateSystemSettingModel
{
    public string? Value { get; init; }
    public string? ValueType { get; init; }
}
