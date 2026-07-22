using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Notifications;

// A notification template (domain-model §4.1). TenantId null = the global default; set = a conjunto
// override (only the override is ITenantOwned — the global default is not scoped). Templates are a
// catalog: created by seeding, edited via API (no create-by-API — §4.2). Uniqueness is on (Type,
// TenantId) at the persistence layer.
public sealed class NotificationTemplate : AggregateRoot
{
    private NotificationTemplate() { } // EF Core

    private NotificationTemplate(
        Guid id,
        Guid? tenantId,
        NotificationType type,
        NotificationChannel channels,
        bool isActive) : base(id)
    {
        TenantId = tenantId;
        Type = type;
        Channels = channels;
        IsActive = isActive;
    }

    public Guid? TenantId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationChannel Channels { get; private set; }
    public string? EmailSubject { get; private set; }
    public string? EmailBodyHtml { get; private set; }
    public string? InAppText { get; private set; }
    public string? PushText { get; private set; }
    public bool IsActive { get; private set; }
    public string? Localization { get; private set; }

    public static NotificationTemplate Create(
        Guid? tenantId,
        NotificationType type,
        NotificationChannel channels,
        bool isActive = true)
        => new(Guid.NewGuid(), tenantId, type, channels, isActive);

    public void UpdateContent(
        NotificationChannel channels,
        string? emailSubject,
        string? emailBodyHtml,
        string? inAppText,
        string? pushText,
        bool isActive,
        string? localization)
    {
        Channels = channels;
        EmailSubject = emailSubject;
        EmailBodyHtml = emailBodyHtml;
        InAppText = inAppText;
        PushText = pushText;
        IsActive = isActive;
        Localization = localization;
    }
}
