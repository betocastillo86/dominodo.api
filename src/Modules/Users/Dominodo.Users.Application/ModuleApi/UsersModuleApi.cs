using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;

namespace Dominodo.Users.Application.ModuleApi;

// Internal implementation of the public Users facade (domain-model §1.7).
// Cross-module callers depend only on IUsersModuleApi from Contracts.
internal sealed class UsersModuleApi(
    IUserRepository users,
    IPlatformRoleAssignmentRepository platformRoleAssignments,
    IPermissionRepository permissions,
    IMembershipRepository memberships,
    IRoleRepository roles) : IUsersModuleApi
{
    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto?> GetUserByPhoneAsync(string phoneE164, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByPhoneAsync(phoneE164, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPlatformPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await platformRoleAssignments.GetByUserAsync(userId, cancellationToken);
        var roleIds = assignments.Select(a => a.RoleId);
        var perms = await permissions.GetByRoleIdsAsync(roleIds, cancellationToken);
        return perms.Select(p => new PermissionDto(p.Id, p.Code, p.Description, p.Group)).ToList();
    }

    public async Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var items = await memberships.ListByUserAsync(userId, cancellationToken);

        var roleNames = new Dictionary<int, string>();
        foreach (var roleId in items.Select(m => m.RoleId).Distinct())
        {
            var role = await roles.GetByIdAsync(roleId, cancellationToken);
            if (role is not null)
            {
                roleNames[roleId] = role.Name;
            }
        }

        return items.Select(m => new MembershipDto(
            m.UserId,
            m.TenantId,
            m.RoleId,
            roleNames.GetValueOrDefault(m.RoleId, string.Empty),
            m.Status.ToString(),
            m.InvitedAtUtc,
            m.JoinedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // (a) platform permissions (same logic as GetPlatformPermissionsAsync).
        var platformAssignments = await platformRoleAssignments.GetByUserAsync(userId, cancellationToken);
        var roleIds = platformAssignments.Select(a => a.RoleId).ToList();

        // (b) the user's Active membership role in this tenant, if any (only Active grants permissions).
        var membership = await memberships.GetActiveByUserAndTenantAsync(userId, tenantId, cancellationToken);
        if (membership is not null)
        {
            roleIds.Add(membership.RoleId);
        }

        var perms = await permissions.GetByRoleIdsAsync(roleIds, cancellationToken);
        return perms
            .GroupBy(p => p.Code)
            .Select(g => g.First())
            .Select(p => new PermissionDto(p.Id, p.Code, p.Description, p.Group))
            .ToList();
    }

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.Phone,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Status.ToString(),
        user.PhoneVerifiedAtUtc is not null);
}
