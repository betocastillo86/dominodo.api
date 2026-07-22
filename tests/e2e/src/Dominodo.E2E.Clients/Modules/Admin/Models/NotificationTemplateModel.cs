namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated response body for the notification-template endpoints. Mirrors the API's
/// <c>NotificationTemplateDto</c> by value. <c>TenantId</c> null = the global default; <c>Type</c> is the
/// <c>NotificationType</c> enum serialized as its name (e.g. "Welcome"). Each channel is an independent
/// on/off toggle; <c>IsActive</c> is the master switch.
/// </summary>
public sealed record NotificationTemplateModel
{
    public Guid Id { get; init; }
    public Guid? TenantId { get; init; }
    public string? Type { get; init; }
    public bool EmailEnabled { get; init; }
    public bool PushEnabled { get; init; }
    public bool InAppEnabled { get; init; }
    public string? EmailSubject { get; init; }
    public string? EmailBodyHtml { get; init; }
    public string? InAppText { get; init; }
    public string? PushText { get; init; }
    public bool IsActive { get; init; }
    public string? Localization { get; init; }
}
