using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;
using FluentValidation;

namespace Dominodo.Users.Application.Roles.UpdateRole;

// Scope is immutable once set (domain-model §1.2) and is therefore not part of the update command.
internal sealed record UpdateRoleCommand(
    int RoleId,
    string Name,
    string? Description,
    IReadOnlyList<int> PermissionIds) : ICommand;

internal sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.PermissionIds).NotEmpty().NotNull();
    }
}

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
