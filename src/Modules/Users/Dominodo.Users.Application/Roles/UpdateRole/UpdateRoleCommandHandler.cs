using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Roles.UpdateRole;

internal sealed class UpdateRoleCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions)
    : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand command, CancellationToken ct)
    {
        var role = await roles.GetByIdAsync(command.RoleId, ct);
        if (role is null)
        {
            return Error.NotFound("Role.NotFound", "Role not found.");
        }

        // System roles are load-bearing (e.g. SuperAdmin carries every permission, which is how it
        // resolves to full access) — they cannot be renamed or reassigned through this endpoint.
        if (role.IsSystem)
        {
            return Error.Forbidden("Role.SystemImmutable", "System roles cannot be modified.");
        }

        if (await roles.ExistsByNameAsync(command.Name.Trim(), command.RoleId, ct))
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

        var renameResult = role.Rename(command.Name, command.Description);
        if (renameResult.IsFailure)
        {
            return renameResult.Error;
        }

        role.AssignPermissions(permissionIds);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
