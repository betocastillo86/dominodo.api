namespace Dominodo.Admin.Contracts;

// Public representation of a push outbox artifact (domain-model §4.2).
public sealed record PushMessageDto(
    Guid Id,
    Guid TenantId,
    Guid RecipientUserId,
    string Title,
    string Body,
    string? TargetUrl,
    string Platform,
    string Status,
    int Attempts,
    string DedupHash,
    DateTimeOffset? SentAtUtc);
