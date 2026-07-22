using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class SystemSettingRepository(AdminDbContext db) : ISystemSettingRepository
{
    public void Add(SystemSetting setting) => db.SystemSettings.Add(setting);

    public Task<SystemSetting?> GetByKeyAsync(string key, Guid? tenantId, CancellationToken cancellationToken = default) =>
        db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key && s.TenantId == tenantId, cancellationToken);

    public async Task<SystemSetting?> ResolveAsync(string key, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is not null)
        {
            var overrideRow = await db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key && s.TenantId == tenantId, cancellationToken);

            if (overrideRow is not null)
            {
                return overrideRow;
            }
        }

        return await db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key && s.TenantId == null, cancellationToken);
    }

    public async Task<(IReadOnlyList<SystemSetting> Items, long TotalCount)> GetAllAsync(Guid? tenantId, string? keyFilter, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = db.SystemSettings.AsNoTracking();

        query = tenantId is not null
            ? query.Where(s => s.TenantId == null || s.TenantId == tenantId)
            : query.Where(s => s.TenantId == null);

        if (!string.IsNullOrWhiteSpace(keyFilter))
        {
            query = query.Where(s => s.Key.Contains(keyFilter));
        }

        query = query.OrderBy(s => s.Key);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> ExistsAsync(string key, Guid? tenantId, CancellationToken cancellationToken = default) =>
        db.SystemSettings.AnyAsync(s => s.Key == key && s.TenantId == tenantId, cancellationToken);
}
