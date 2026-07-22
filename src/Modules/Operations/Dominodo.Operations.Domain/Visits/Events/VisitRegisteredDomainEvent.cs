using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Visits.Events;

public sealed record VisitRegisteredDomainEvent(
    Guid VisitId,
    Guid TenantId,
    Guid ApartmentId,
    Guid RegisteredByUserId) : IDomainEvent;
