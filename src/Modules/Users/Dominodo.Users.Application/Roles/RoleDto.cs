using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Application.Roles;

internal sealed record RoleDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    RoleScope Scope,
    IReadOnlyList<int> PermissionIds);
