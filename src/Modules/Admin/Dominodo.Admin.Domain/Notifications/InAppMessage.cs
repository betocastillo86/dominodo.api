using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Notifications;

// A materialized in-app notification (domain-model §4.2). NOT ITenantOwned: TenantId is a plain column
// (used to pick sender/reporting), queried by RecipientUserId — never scoped via ForCurrentTenant.
public sealed class InAppMessage : AggregateRoot
{
    private InAppMessage() { } // EF Core

    private InAppMessage(
        Guid id,
        Guid tenantId,
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        string? targetUrl,
        Guid? triggeredByUserId,
        DateTimeOffset createdAtUtc) : base(id)
    {
        TenantId = tenantId;
        RecipientUserId = recipientUserId;
        Type = type;
        Title = title;
        Body = body;
        TargetUrl = targetUrl;
        TriggeredByUserId = triggeredByUserId;
        IsRead = false;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string? TargetUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public Guid? TriggeredByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<InAppMessage> Create(
        Guid tenantId,
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        string? targetUrl,
        Guid? triggeredByUserId,
        IClock clock)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("InAppMessage.TenantRequired", "A tenant is required.");
        }

        if (recipientUserId == Guid.Empty)
        {
            return Error.Validation("InAppMessage.RecipientRequired", "A recipient is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("InAppMessage.TitleRequired", "A title is required.");
        }

        return new InAppMessage(
            Guid.NewGuid(), tenantId, recipientUserId, type, title.Trim(), body ?? string.Empty,
            string.IsNullOrWhiteSpace(targetUrl) ? null : targetUrl.Trim(), triggeredByUserId, clock.UtcNow);
    }

    public void MarkRead(IClock clock)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = clock.UtcNow;
    }
}
