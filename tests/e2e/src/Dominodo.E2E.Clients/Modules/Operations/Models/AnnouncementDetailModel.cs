namespace Dominodo.E2E.Clients.Modules.Operations.Models;

/// <summary>
/// Hand-replicated response body for <c>GET /api/v1/announcements/{id}</c>. Mirrors the API's
/// <c>AnnouncementDetailDto</c> by value (adds Body, AudienceFilter and PublishedByUserId over the
/// list DTO).
/// </summary>
public sealed record AnnouncementDetailModel
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Title { get; init; } = default!;
    public string Body { get; init; } = default!;
    public string? Category { get; init; }
    public byte Priority { get; init; }
    public string Status { get; init; } = default!;
    public string AudienceType { get; init; } = default!;
    public string? AudienceFilter { get; init; }
    public DateTimeOffset? PublishedAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public Guid? PublishedByUserId { get; init; }
}
