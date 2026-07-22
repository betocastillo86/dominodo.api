namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record VisitRegisteredIntegrationEvent(
    Guid VisitId,
    Guid TenantId,
    Guid ApartmentId,
    Guid RegisteredByUserId);
