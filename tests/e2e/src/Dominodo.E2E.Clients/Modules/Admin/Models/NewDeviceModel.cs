namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/devices</c>. Mirrors the API's
/// <c>RegisterDeviceRequest</c> by value. <c>Platform</c> must parse to the <c>DevicePlatform</c> enum
/// ("Android" or "iOS"); <c>Token</c> is required and at most 512 chars.
/// </summary>
public sealed record NewDeviceModel
{
    public string? Platform { get; init; }
    public string? Token { get; init; }
}
