using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Notifications;

// A notification template (domain-model §4.1). TenantId null = the global default; set = a conjunto
// override (only the override is ITenantOwned — the global default is not scoped). Templates are a
// catalog: created by seeding, edited via API (no create-by-API — §4.2). Uniqueness is on (Type,
// TenantId) at the persistence layer. Each channel is independently enabled/disabled.
public sealed class NotificationTemplate : AggregateRoot
{
    private NotificationTemplate() { } // EF Core

    private NotificationTemplate(
        Guid id,
        Guid? tenantId,
        NotificationType type,
        bool emailEnabled,
        bool pushEnabled,
        bool inAppEnabled,
        bool isActive) : base(id)
    {
        TenantId = tenantId;
        Type = type;
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        InAppEnabled = inAppEnabled;
        IsActive = isActive;
    }

    public Guid? TenantId { get; private set; }
    public NotificationType Type { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public string? EmailSubject { get; private set; }
    public string? EmailBodyHtml { get; private set; }
    public string? InAppText { get; private set; }
    public string? PushText { get; private set; }
    public bool IsActive { get; private set; }
    public string? Localization { get; private set; }

    public static NotificationTemplate Create(
        Guid? tenantId,
        NotificationType type,
        bool emailEnabled,
        bool pushEnabled,
        bool inAppEnabled,
        bool isActive = true)
        => new(Guid.NewGuid(), tenantId, type, emailEnabled, pushEnabled, inAppEnabled, isActive);

    public void UpdateContent(
        bool emailEnabled,
        string? emailSubject,
        string? emailBodyHtml,
        bool pushEnabled,
        string? pushText,
        bool inAppEnabled,
        string? inAppText,
        bool isActive,
        string? localization)
    {
        EmailEnabled = emailEnabled;
        EmailSubject = emailSubject;
        EmailBodyHtml = emailBodyHtml;
        PushEnabled = pushEnabled;
        PushText = pushText;
        InAppEnabled = inAppEnabled;
        InAppText = inAppText;
        IsActive = isActive;
        Localization = localization;
    }
}
