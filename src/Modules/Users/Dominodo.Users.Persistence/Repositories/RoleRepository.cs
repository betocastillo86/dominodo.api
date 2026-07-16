using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

internal sealed class RoleRepository(UsersDbContext db) : IRoleRepository
{
    public void Add(Role role) => db.Roles.Add(role);

    public Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        db.Roles.AnyAsync(r => r.Name == name, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, int excludeRoleId, CancellationToken cancellationToken = default) =>
        db.Roles.AnyAsync(r => r.Name == name && r.Id != excludeRoleId, cancellationToken);

    public async Task<(IReadOnlyList<Role> Items, long TotalCount)> ListAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Roles.Include(r => r.Permissions).OrderBy(r => r.Id);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<int> GetMaxIdAsync(CancellationToken cancellationToken = default) =>
        await db.Roles.AnyAsync(cancellationToken)
            ? await db.Roles.MaxAsync(r => r.Id, cancellationToken)
            : 0;
}
