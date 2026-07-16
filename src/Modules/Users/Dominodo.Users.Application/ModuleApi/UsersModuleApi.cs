using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;

namespace Dominodo.Users.Application.ModuleApi;

// Internal implementation of the public Users facade (domain-model §1.7).
// Cross-module callers depend only on IUsersModuleApi from Contracts.
internal sealed class UsersModuleApi(
    IUserRepository users,
    IPlatformRoleAssignmentRepository platformRoleAssignments,
    IPermissionRepository permissions) : IUsersModuleApi
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

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.Phone,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Status.ToString(),
        user.PhoneVerifiedAtUtc is not null);
}
