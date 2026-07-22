namespace Dominodo.Operations.Contracts;

public sealed record AnnouncementDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Category,
    byte Priority,
    string Status,
    string AudienceType,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
