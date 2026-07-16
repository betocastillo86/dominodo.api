using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Application.Roles.CreateRole;

internal sealed class CreateRoleCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions)
    : ICommandHandler<CreateRoleCommand, int>
{
    public async Task<Result<int>> Handle(CreateRoleCommand command, CancellationToken ct)
    {
        var scope = Enum.Parse<RoleScope>(command.Scope);

        if (await roles.ExistsByNameAsync(command.Name.Trim(), ct))
        {
            return Error.Conflict("Role.NameAlreadyExists", "A role with this name already exists.");
        }

        var permissionIds = command.PermissionIds.Distinct().ToList();
        if (permissionIds.Count > 0)
        {
            var existing = await permissions.GetExistingIdsAsync(permissionIds, ct);
            var missing = permissionIds.Except(existing).ToList();
            if (missing.Count > 0)
            {
                return Error.Validation(
                    "Role.InvalidPermissions",
                    $"Unknown permission ids: {string.Join(", ", missing)}.");
            }
        }

        var nextId = await roles.GetMaxIdAsync(ct) + 1;

        var roleResult = Role.Create(nextId, command.Name, command.Description, scope);
        if (roleResult.IsFailure)
        {
            return roleResult.Error;
        }

        var role = roleResult.Value;
        role.AssignPermissions(permissionIds);
        roles.Add(role);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return role.Id;
    }
}
