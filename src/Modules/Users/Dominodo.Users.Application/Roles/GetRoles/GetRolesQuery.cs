using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Application.Roles.GetRoles;

internal sealed record GetRolesQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<RoleDto>>;

internal sealed class GetRolesQueryHandler(IRoleRepository roles)
    : IQueryHandler<GetRolesQuery, PagedResult<RoleDto>>
{
    public async Task<Result<PagedResult<RoleDto>>> Handle(GetRolesQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await roles.ListAsync(page, ct);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<RoleDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }

    private static RoleDto ToDto(Role role) => new(
        role.Id,
        role.Name,
        role.Description,
        role.IsSystem,
        role.Scope,
        role.Permissions.Select(p => p.PermissionId).ToList());
}
