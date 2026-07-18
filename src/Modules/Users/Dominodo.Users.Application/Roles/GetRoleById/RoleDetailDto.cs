namespace Dominodo.Users.Application.Roles.GetRoleById;

internal sealed record RolePermissionSummaryDto(int Id, string Code);

internal sealed record RoleDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    string Scope,
    IReadOnlyList<RolePermissionSummaryDto> Permissions);
