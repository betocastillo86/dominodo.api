namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/notification-templates/{id}</c>. Mirrors the API's
/// <c>UpdateNotificationTemplateRequest</c> by value. Enabling a channel requires its content to be
/// present (validator): EmailEnabled ⇒ EmailSubject + EmailBodyHtml, PushEnabled ⇒ PushText,
/// InAppEnabled ⇒ InAppText. Override any field via <c>model with { ... }</c> for the 400 cases.
/// </summary>
public sealed record UpdateNotificationTemplateModel
{
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
