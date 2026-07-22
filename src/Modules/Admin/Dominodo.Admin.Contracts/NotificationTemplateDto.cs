namespace Dominodo.Admin.Contracts;

// Public representation of a notification template (domain-model §4.1). TenantId null = global default.
// Each channel is an independent on/off toggle; IsActive is the master switch.
public sealed record NotificationTemplateDto(
    Guid Id,
    Guid? TenantId,
    string Type,
    bool EmailEnabled,
    bool PushEnabled,
    bool InAppEnabled,
    string? EmailSubject,
    string? EmailBodyHtml,
    string? InAppText,
    string? PushText,
    bool IsActive,
    string? Localization);
