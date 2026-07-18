using Dominodo.Shared.Kernel;

namespace Dominodo.Tenants.Domain.Apartments.Events;

public sealed record ResidentRemovedFromApartmentDomainEvent(
    Guid ApartmentId,
    Guid TenantId,
    Guid ResidentId,
    Guid UserId) : IDomainEvent;
