using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;

namespace Dominodo.Tenants.Application.ModuleApi;

// Internal implementation of the public Tenants facade (domain-model §2.5). Cross-module callers depend
// only on ITenantsModuleApi from Contracts. Mirrors UsersModuleApi: delegates straight to the domain
// ports (no MediatR round-trip for reads).
internal sealed class TenantsModuleApi(
    ITenantRepository tenants,
    IApartmentRepository apartments) : ITenantsModuleApi
{
    public async Task<TenantDto?> GetTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await tenants.GetByIdAsync(id, cancellationToken);
        return tenant is null ? null : ToDto(tenant);
    }

    public async Task<ApartmentDto?> GetApartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Scoped to the caller's current tenant (repository funnels through ForCurrentTenant).
        var apartment = await apartments.GetByIdAsync(id, cancellationToken);
        return apartment is null ? null : ToDto(apartment);
    }

    public Task<bool> ApartmentExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default) =>
        apartments.ExistsForTenantAsync(id, tenantId, cancellationToken);

    public async Task<IReadOnlyList<ResidentDto>> GetApartmentResidentsAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        var apartment = await apartments.GetByIdWithResidentsAsync(apartmentId, cancellationToken);
        if (apartment is null)
        {
            return [];
        }

        return apartment.Residents.Select(ToDto).ToList();
    }

    public async Task<bool> IsFeatureEnabledAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<FeatureKey>(featureKey, ignoreCase: false, out var key))
        {
            return false;
        }

        var tenant = await tenants.GetByIdWithFeaturesAsync(tenantId, cancellationToken);
        return tenant?.IsFeatureEnabled(key) ?? false;
    }

    public async Task<IReadOnlyList<ResidentApartmentDto>> GetApartmentsForResidentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Scoped to the caller's current tenant (repository funnels through ForCurrentTenant).
        var residentApartments = await apartments.ListForResidentAsync(userId, cancellationToken);
        return residentApartments.Select(a => new ResidentApartmentDto(a.Id, a.Tower)).ToList();
    }

    private static TenantDto ToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Slug,
        tenant.Name,
        tenant.Type.ToString(),
        tenant.Status.ToString(),
        tenant.City);

    private static ApartmentDto ToDto(Apartment apartment) => new(
        apartment.Id,
        apartment.TenantId,
        apartment.Tower,
        apartment.Number,
        apartment.Type.ToString(),
        apartment.Status.ToString());

    private static ResidentDto ToDto(ApartmentResident r) => new(
        r.Id,
        r.ApartmentId,
        r.UserId,
        r.RelationType.ToString(),
        r.LivesHere,
        r.StartDate,
        r.EndDate,
        r.IsActive);
}
