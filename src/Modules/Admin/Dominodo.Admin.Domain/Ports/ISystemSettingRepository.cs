using Dominodo.Admin.Domain.Configuration;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Domain.Ports;

public interface ISystemSettingRepository
{
    void Add(SystemSetting setting);

    // Exact match on (Key, TenantId). A null tenantId targets the global row.
    Task<SystemSetting?> GetByKeyAsync(string key, Guid? tenantId, CancellationToken cancellationToken = default);

    // Resolution read (domain-model §4.4): the tenant override if present, otherwise the global row.
    Task<SystemSetting?> ResolveAsync(string key, Guid? tenantId, CancellationToken cancellationToken = default);

    // Lists global rows plus, when tenantId is set, that tenant's overrides. When keyFilter is set,
    // restricts to rows whose Key contains it (case-insensitive substring match).
    Task<(IReadOnlyList<SystemSetting> Items, long TotalCount)> GetAllAsync(Guid? tenantId, string? keyFilter, PageRequest page, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, Guid? tenantId, CancellationToken cancellationToken = default);
}
