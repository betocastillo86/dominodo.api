namespace Dominodo.Tenants.Contracts;

// The apartments (and their towers) a user is an ACTIVE resident of, within the current tenant. Used by
// Operations to narrow announcement audiences (ByTower/ByApartments) to the caller (domain-model §3.4).
public sealed record ResidentApartmentDto(Guid ApartmentId, string? Tower);
