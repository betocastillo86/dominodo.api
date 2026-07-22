namespace Dominodo.Tenants.Contracts;

// Public synchronous read surface of the Tenants module (domain-model §2.5). Other modules depend only
// on this interface from Contracts. Tenant-scoped reads (GetApartment, GetApartmentResidents) resolve
// against the caller's current tenant; ApartmentExists/IsFeatureEnabled take an explicit TenantId.
public interface ITenantsModuleApi
{
    Task<TenantDto?> GetTenantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApartmentDto?> GetApartmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ApartmentExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResidentDto>> GetApartmentResidentsAsync(Guid apartmentId, CancellationToken cancellationToken = default);
    Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken = default);

    // The apartments (+ towers) the user is an ACTIVE resident of, scoped to the caller's current tenant.
    // Powers announcement audience matching (ByTower/ByApartments) in Operations (domain-model §3.4).
    Task<IReadOnlyList<ResidentApartmentDto>> GetApartmentsForResidentAsync(Guid userId, CancellationToken cancellationToken = default);
}
