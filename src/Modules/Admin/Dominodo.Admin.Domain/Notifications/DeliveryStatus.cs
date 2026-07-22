namespace Dominodo.Admin.Domain.Notifications;

public enum DeliveryStatus
{
    // Pending applies to the materialized outbox messages (§4.2); the OTP NotificationDelivery audit
    // (§4.2 minimal slice) only ever records Sent/Failed.
    Pending,
    Sent,
    Failed
}
