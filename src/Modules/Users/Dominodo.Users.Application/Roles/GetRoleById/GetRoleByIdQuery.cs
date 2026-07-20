using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Roles.GetRoleById;

internal sealed record GetRoleByIdQuery(int RoleId) : IQuery<RoleDetailDto>;

internal sealed class GetRoleByIdQueryHandler(IRoleRepository roles, IPermissionRepository permissions)
    : IQueryHandler<GetRoleByIdQuery, RoleDetailDto>
{
    public async Task<Result<RoleDetailDto>> Handle(GetRoleByIdQuery query, CancellationToken ct)
    {
        var role = await roles.GetByIdAsync(query.RoleId, ct);

        if (role is null)
        {
            return Error.NotFound("Role.NotFound", "Role not found.");
        }

        var rolePermissions = await permissions.GetByRoleIdsAsync([role.Id], ct);

        return new RoleDetailDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            role.Scope.ToString(),
            rolePermissions.Select(p => new RolePermissionSummaryDto(p.Id, p.Code)).ToList());
    }
}
