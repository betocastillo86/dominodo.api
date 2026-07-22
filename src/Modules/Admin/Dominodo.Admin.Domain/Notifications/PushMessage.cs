using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Notifications;

// A materialized push outbox artifact (domain-model §4.2). NOT ITenantOwned: TenantId is a plain column,
// queried by recipient/status. DedupHash guards against duplicate sends.
public sealed class PushMessage : AggregateRoot
{
    private PushMessage() { } // EF Core

    private PushMessage(
        Guid id,
        Guid tenantId,
        Guid recipientUserId,
        string title,
        string body,
        string? targetUrl,
        DevicePlatform platform,
        DeliveryStatus status,
        string dedupHash) : base(id)
    {
        TenantId = tenantId;
        RecipientUserId = recipientUserId;
        Title = title;
        Body = body;
        TargetUrl = targetUrl;
        Platform = platform;
        Status = status;
        Attempts = 0;
        DedupHash = dedupHash;
    }

    public Guid TenantId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string? TargetUrl { get; private set; }
    public DevicePlatform Platform { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string DedupHash { get; private set; } = null!;
    public DateTimeOffset? SentAtUtc { get; private set; }

    public static Result<PushMessage> Create(
        Guid tenantId,
        Guid recipientUserId,
        string title,
        string body,
        string? targetUrl,
        DevicePlatform platform,
        string dedupHash)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("PushMessage.TenantRequired", "A tenant is required.");
        }

        if (recipientUserId == Guid.Empty)
        {
            return Error.Validation("PushMessage.RecipientRequired", "A recipient is required.");
        }

        return new PushMessage(
            Guid.NewGuid(), tenantId, recipientUserId, title ?? string.Empty, body ?? string.Empty,
            string.IsNullOrWhiteSpace(targetUrl) ? null : targetUrl.Trim(), platform, DeliveryStatus.Pending,
            dedupHash ?? string.Empty);
    }

    public void MarkSent(IClock clock)
    {
        Status = DeliveryStatus.Sent;
        Attempts += 1;
        SentAtUtc = clock.UtcNow;
    }

    public void MarkFailed()
    {
        Status = DeliveryStatus.Failed;
        Attempts += 1;
    }
}
