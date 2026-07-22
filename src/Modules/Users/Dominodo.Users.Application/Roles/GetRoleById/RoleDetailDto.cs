using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Application.Roles.GetRoleById;

internal sealed record RolePermissionSummaryDto(int Id, string Code);

internal sealed record RoleDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    RoleScope Scope,
    IReadOnlyList<RolePermissionSummaryDto> Permissions);
