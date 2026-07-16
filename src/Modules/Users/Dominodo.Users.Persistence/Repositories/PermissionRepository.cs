using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

internal sealed class PermissionRepository(UsersDbContext db) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Permissions.AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> GetExistingIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToList();
        return await db.Permissions
            .Where(p => idSet.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetByRoleIdsAsync(
        IEnumerable<int> roleIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = roleIds.ToList();
        return await db.Permissions
            .Where(p => db.RolePermissions.Any(rp => rp.PermissionId == p.Id && idSet.Contains(rp.RoleId)))
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
