using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Roles.CreateRole;

internal sealed record CreateRoleCommand(
    string Name,
    string? Description,
    string Scope,
    IReadOnlyList<int> PermissionIds) : ICommand<int>;
