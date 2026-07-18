namespace Dominodo.Tenants.Contracts.IntegrationEvents;

// Published when a new conjunto is registered. Consumed by other modules (e.g. Admin seeds default
// config/templates for the tenant). Slug is included so consumers avoid a round-trip to resolve it.
public sealed record TenantCreatedIntegrationEvent(Guid TenantId, string Slug);
