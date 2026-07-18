namespace Dominodo.Tenants.Contracts.IntegrationEvents;

public sealed record ApartmentCreatedIntegrationEvent(Guid ApartmentId, Guid TenantId);
