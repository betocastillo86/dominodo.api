using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Notifications;

// A materialized email outbox artifact (domain-model §4.2). NOT ITenantOwned: TenantId is a plain column,
// queried by recipient/status. Distinct from Shared.Abstractions.EmailMessage (the transport record used
// by IEmailSender) — same name, different namespace, kept separate on purpose.
public sealed class EmailMessage : AggregateRoot
{
    private EmailMessage() { } // EF Core

    private EmailMessage(
        Guid id,
        Guid tenantId,
        string to,
        string? toName,
        string subject,
        string bodyHtml,
        byte priority,
        DeliveryStatus status,
        DateTimeOffset? scheduledAtUtc) : base(id)
    {
        TenantId = tenantId;
        To = to;
        ToName = toName;
        Subject = subject;
        BodyHtml = bodyHtml;
        Priority = priority;
        Status = status;
        Attempts = 0;
        ScheduledAtUtc = scheduledAtUtc;
    }

    public Guid TenantId { get; private set; }
    public string To { get; private set; } = null!;
    public string? ToName { get; private set; }
    public string Subject { get; private set; } = null!;
    public string BodyHtml { get; private set; } = null!;
    public byte Priority { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }

    public static Result<EmailMessage> Create(
        Guid tenantId,
        string to,
        string? toName,
        string subject,
        string bodyHtml,
        byte priority,
        DateTimeOffset? scheduledAtUtc = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("EmailMessage.TenantRequired", "A tenant is required.");
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return Error.Validation("EmailMessage.RecipientRequired", "A recipient email is required.");
        }

        return new EmailMessage(
            Guid.NewGuid(), tenantId, to.Trim(), toName, subject ?? string.Empty, bodyHtml ?? string.Empty,
            priority, DeliveryStatus.Pending, scheduledAtUtc);
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
