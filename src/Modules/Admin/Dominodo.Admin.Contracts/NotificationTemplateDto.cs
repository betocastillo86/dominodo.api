namespace Dominodo.Admin.Contracts;

// Public representation of a notification template (domain-model §4.1). TenantId null = global default.
// Channels is the [Flags] value rendered as a comma-separated string (e.g. "Email, InApp").
public sealed record NotificationTemplateDto(
    Guid Id,
    Guid? TenantId,
    string Type,
    string Channels,
    string? EmailSubject,
    string? EmailBodyHtml,
    string? InAppText,
    string? PushText,
    bool IsActive,
    string? Localization);
