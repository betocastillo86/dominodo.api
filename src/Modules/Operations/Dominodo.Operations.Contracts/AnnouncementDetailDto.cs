namespace Dominodo.Operations.Contracts;

public sealed record AnnouncementDetailDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string Body,
    string? Category,
    byte Priority,
    string Status,
    string AudienceType,
    string? AudienceFilter,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    Guid? PublishedByUserId);
