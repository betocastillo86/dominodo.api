namespace Dominodo.Admin.Contracts;

// Public representation of an email outbox artifact (domain-model §4.2).
public sealed record EmailMessageDto(
    Guid Id,
    Guid TenantId,
    string To,
    string? ToName,
    string Subject,
    string BodyHtml,
    byte Priority,
    string Status,
    int Attempts,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? SentAtUtc);
