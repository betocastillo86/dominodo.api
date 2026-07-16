namespace Dominodo.Users.Application.Roles;

internal sealed record RoleDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    string Scope,
    IReadOnlyList<int> PermissionIds);
