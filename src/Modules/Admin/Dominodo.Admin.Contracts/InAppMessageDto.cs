namespace Dominodo.Admin.Contracts;

// Public representation of a materialized in-app notification (domain-model §4.2).
public sealed record InAppMessageDto(
    Guid Id,
    Guid TenantId,
    Guid RecipientUserId,
    string Type,
    string Title,
    string Body,
    string? TargetUrl,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    Guid? TriggeredByUserId,
    DateTimeOffset CreatedAtUtc);
