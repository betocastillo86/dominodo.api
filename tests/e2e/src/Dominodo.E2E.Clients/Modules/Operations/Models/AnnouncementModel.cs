namespace Dominodo.E2E.Clients.Modules.Operations.Models;

/// <summary>
/// Hand-replicated response body for the announcement list / <c>/mine</c> feed. Mirrors the API's
/// <c>AnnouncementDto</c> by value. <c>Status</c> and <c>AudienceType</c> come back as enum names.
/// </summary>
public sealed record AnnouncementModel
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Title { get; init; } = default!;
    public string? Category { get; init; }
    public byte Priority { get; init; }
    public string Status { get; init; } = default!;
    public string AudienceType { get; init; } = default!;
    public DateTimeOffset? PublishedAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
