using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Domain.Apartments;

namespace Dominodo.Tenants.Domain.Ports;

// All reads are implicitly scoped to the current tenant by the implementation (ForCurrentTenant, doc 09),
// so callers never pass a TenantId — cross-tenant leakage is impossible by construction.
public interface IApartmentRepository
{
    void Add(Apartment apartment);
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Apartment?> GetByIdWithResidentsAsync(Guid id, CancellationToken cancellationToken = default);

    // Facade existence check with an EXPLICIT tenant (not the request context) — the caller passes the
    // TenantId, and both must match, so there is no cross-tenant leakage. Used by ITenantsModuleApi.
    Task<bool> ExistsForTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTowerAndNumberAsync(string? tower, string number, CancellationToken cancellationToken = default);

    // Apartments (current tenant) the user is an ACTIVE resident of. Backs the facade's audience resolution.
    Task<IReadOnlyList<Apartment>> ListForResidentAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Apartment> Items, long TotalCount)> ListAsync(
        PageRequest page,
        string? tower,
        ApartmentType? type,
        ApartmentStatus? status,
        CancellationToken cancellationToken = default);
}
