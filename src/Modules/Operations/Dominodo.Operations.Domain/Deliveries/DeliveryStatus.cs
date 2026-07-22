namespace Dominodo.Operations.Domain.Deliveries;

// Lifecycle (domain-model §3.2): Received → Notified → Delivered | Returned.
public enum DeliveryStatus
{
    Received,
    Notified,
    Delivered,
    Returned
}
