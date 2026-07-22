namespace Dominodo.E2E.Clients.Modules.Operations.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/announcements</c>. Mirrors the API's
/// <c>CreateAnnouncementRequest</c> by value. <c>AudienceType</c> is an <c>AudienceType</c> name
/// ("AllTenant", "ByTower", "ByApartments"), serialized as a string by the global enum converter.
/// <c>AudienceFilter</c> is raw JSON: a <c>string[]</c> of towers for ByTower, a <c>Guid[]</c> of
/// apartment ids for ByApartments; ignored for AllTenant.
/// </summary>
public sealed record NewAnnouncementModel
{
    public string Title { get; init; } = default!;
    public string Body { get; init; } = default!;
    public byte Priority { get; init; }
    public string AudienceType { get; init; } = default!;
    public string? AudienceFilter { get; init; }
    public string? Category { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
