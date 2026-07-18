using Dominodo.Shared.Kernel;

namespace Dominodo.Tenants.Domain.Apartments.Events;

public sealed record ApartmentCreatedDomainEvent(Guid ApartmentId, Guid TenantId) : IDomainEvent;
