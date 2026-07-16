using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

internal sealed class PlatformRoleAssignmentRepository(UsersDbContext db) : IPlatformRoleAssignmentRepository
{
    public async Task<IReadOnlyList<PlatformRoleAssignment>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await db.PlatformRoleAssignments
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetPlatformRoleNamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await db.PlatformRoleAssignments
            .Where(a => a.UserId == userId)
            .Join(db.Roles.Where(r => r.Scope == RoleScope.Platform),
                a => a.RoleId,
                r => r.Id,
                (_, r) => r.Name)
            .ToListAsync(cancellationToken);

    public void Add(PlatformRoleAssignment assignment) =>
        db.PlatformRoleAssignments.Add(assignment);

    public Task<bool> ExistsAsync(
        Guid userId,
        int roleId,
        CancellationToken cancellationToken = default) =>
        db.PlatformRoleAssignments
            .AnyAsync(a => a.UserId == userId && a.RoleId == roleId, cancellationToken);
}
