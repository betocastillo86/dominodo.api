using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Users.Application.Roles.GetRoles;

internal sealed record GetRolesQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<RoleDto>>;
