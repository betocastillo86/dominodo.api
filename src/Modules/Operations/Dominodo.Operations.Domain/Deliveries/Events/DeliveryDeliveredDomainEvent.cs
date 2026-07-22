using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Deliveries.Events;

public sealed record DeliveryDeliveredDomainEvent(
    Guid DeliveryId,
    Guid TenantId,
    Guid ApartmentId,
    DateTimeOffset DeliveredAtUtc,
    Guid? DeliveredToUserId) : IDomainEvent;
