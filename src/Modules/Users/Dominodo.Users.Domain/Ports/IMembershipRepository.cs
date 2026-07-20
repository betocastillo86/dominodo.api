using Dominodo.Users.Domain.Memberships;

namespace Dominodo.Users.Domain.Ports;

public interface IMembershipRepository
{
    void Add(Membership membership);

    // Explicit ids, unscoped — facade + invite duplicate check.
    Task<Membership?> GetByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    // Explicit ids, unscoped — permission resolution (only Active grants permissions).
    Task<Membership?> GetActiveByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    // Cross-tenant — facade GetMemberships.
    Task<IReadOnlyList<Membership>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Tenant-scoped — mutations from the tenant controller.
    Task<Membership?> GetByIdForCurrentTenantAsync(Guid id, CancellationToken cancellationToken = default);

    // Tenant-scoped — controller list.
    Task<IReadOnlyList<Membership>> ListForCurrentTenantAsync(CancellationToken cancellationToken = default);

    void Remove(Membership membership);
}
