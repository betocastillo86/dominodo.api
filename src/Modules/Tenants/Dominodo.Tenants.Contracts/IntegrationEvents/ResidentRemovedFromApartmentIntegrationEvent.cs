namespace Dominodo.Tenants.Contracts.IntegrationEvents;

public sealed record ResidentRemovedFromApartmentIntegrationEvent(
    Guid ApartmentId,
    Guid TenantId,
    Guid ResidentId,
    Guid UserId);
