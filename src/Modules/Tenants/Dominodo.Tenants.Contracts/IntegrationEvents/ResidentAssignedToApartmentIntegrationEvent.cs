namespace Dominodo.Tenants.Contracts.IntegrationEvents;

public sealed record ResidentAssignedToApartmentIntegrationEvent(
    Guid ApartmentId,
    Guid TenantId,
    Guid ResidentId,
    Guid UserId);
