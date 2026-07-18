using Dominodo.Shared.Kernel;

namespace Dominodo.Tenants.Domain.Apartments.Events;

public sealed record ResidentAssignedToApartmentDomainEvent(
    Guid ApartmentId,
    Guid TenantId,
    Guid ResidentId,
    Guid UserId) : IDomainEvent;
