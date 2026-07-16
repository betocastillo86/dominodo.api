using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Roles.UpdateRole;

// Scope is immutable once set (domain-model §1.2) and is therefore not part of the update command.
internal sealed record UpdateRoleCommand(
    int RoleId,
    string Name,
    string? Description,
    IReadOnlyList<int> PermissionIds) : ICommand;
