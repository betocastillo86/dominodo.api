using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;

namespace Dominodo.Tenants.Application.Tenants.GetTenants;

internal sealed class GetTenantsQueryHandler(ITenantRepository tenants)
    : IQueryHandler<GetTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<Result<PagedResult<TenantDto>>> Handle(GetTenantsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await tenants.ListAsync(page, ct);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<TenantDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }

    private static TenantDto ToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Slug,
        tenant.Name,
        tenant.Type.ToString(),
        tenant.Status.ToString(),
        tenant.City);
}
