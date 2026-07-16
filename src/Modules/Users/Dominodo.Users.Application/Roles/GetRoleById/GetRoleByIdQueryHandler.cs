using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Roles.GetRoleById;

internal sealed class GetRoleByIdQueryHandler(IRoleRepository roles)
    : IQueryHandler<GetRoleByIdQuery, RoleDto>
{
    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery query, CancellationToken ct)
    {
        var role = await roles.GetByIdAsync(query.RoleId, ct);

        return role is null
            ? Error.NotFound("Role.NotFound", "Role not found.")
            : new RoleDto(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystem,
                role.Scope.ToString(),
                role.Permissions.Select(p => p.PermissionId).ToList());
    }
}
