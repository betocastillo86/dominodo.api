using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Tenants.Application.Tenants.GetTenants;

internal sealed record GetTenantsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<TenantDto>>;
