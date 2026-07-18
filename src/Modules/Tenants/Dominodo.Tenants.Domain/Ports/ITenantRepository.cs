using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Domain.Tenants;

namespace Dominodo.Tenants.Domain.Ports;

public interface ITenantRepository
{
    void Add(Tenant tenant);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdWithFeaturesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Tenant> Items, long TotalCount)> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
}
