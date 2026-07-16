using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Notifications;

// Minimal audit record of a transactional message the Admin module delivered (domain-model §4.2,
// kept minimal for the OTP slice). Carries the source integration-event id for idempotency.
public sealed class NotificationDelivery : AggregateRoot
{
    private NotificationDelivery() { } // EF Core

    private NotificationDelivery(
        Guid id,
        Guid sourceEventId,
        DeliveryChannel channel,
        string recipient,
        string purpose,
        DeliveryStatus status,
        DateTimeOffset createdAtUtc) : base(id)
    {
        SourceEventId = sourceEventId;
        Channel = channel;
        Recipient = recipient;
        Purpose = purpose;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid SourceEventId { get; private set; }
    public DeliveryChannel Channel { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string Purpose { get; private set; } = null!;
    public DeliveryStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static NotificationDelivery Record(
        Guid sourceEventId,
        DeliveryChannel channel,
        string recipient,
        string purpose,
        DeliveryStatus status,
        IClock clock)
        => new(Guid.NewGuid(), sourceEventId, channel, recipient, purpose, status, clock.UtcNow);
}
