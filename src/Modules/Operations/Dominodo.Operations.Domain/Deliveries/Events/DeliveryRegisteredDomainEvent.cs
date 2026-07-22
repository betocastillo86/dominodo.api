using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Deliveries.Events;

public sealed record DeliveryRegisteredDomainEvent(
    Guid DeliveryId,
    Guid TenantId,
    string Code,
    Guid ApartmentId,
    Guid RegisteredByUserId) : IDomainEvent;
