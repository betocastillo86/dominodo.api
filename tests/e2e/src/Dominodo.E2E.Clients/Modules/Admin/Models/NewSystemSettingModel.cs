namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/system-settings</c>. Mirrors the API's
/// <c>CreateSystemSettingRequest</c> by value. <c>Key</c> is required (≤ 200 chars); <c>Value</c> is
/// required (non-null); <c>ValueType</c> must parse to the <c>SystemSettingValueType</c> enum
/// ("String", "Int", "Bool" or "Json"). Any field is overridable for the 400 cases via
/// <c>model with { ... }</c>.
/// </summary>
public sealed record NewSystemSettingModel
{
    public string? Key { get; init; }
    public string? Value { get; init; }
    public string? ValueType { get; init; }
}
