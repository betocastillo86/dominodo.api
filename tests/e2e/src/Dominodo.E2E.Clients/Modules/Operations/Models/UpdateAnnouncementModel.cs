namespace Dominodo.E2E.Clients.Modules.Operations.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/announcements/{id}</c>. Mirrors the API's
/// <c>UpdateAnnouncementRequest</c> by value (same shape as <see cref="NewAnnouncementModel"/>; the
/// id travels in the route, not the body).
/// </summary>
public sealed record UpdateAnnouncementModel
{
    public string Title { get; init; } = default!;
    public string Body { get; init; } = default!;
    public byte Priority { get; init; }
    public string AudienceType { get; init; } = default!;
    public string? AudienceFilter { get; init; }
    public string? Category { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
