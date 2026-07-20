using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Tenants.GetTenantById;

internal sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDetailDto>;

internal sealed class GetTenantByIdQueryHandler(ITenantRepository tenants)
    : IQueryHandler<GetTenantByIdQuery, TenantDetailDto>
{
    public async Task<Result<TenantDetailDto>> Handle(GetTenantByIdQuery query, CancellationToken ct)
    {
        var tenant = await tenants.GetByIdAsync(query.TenantId, ct);
        if (tenant is null)
        {
            return Error.NotFound("Tenant.NotFound", "Tenant not found.");
        }

        return new TenantDetailDto(
            tenant.Id,
            tenant.Slug,
            tenant.Name,
            tenant.LegalId,
            tenant.Type.ToString(),
            tenant.Status.ToString(),
            tenant.Address,
            tenant.City,
            tenant.Country,
            tenant.Branding,
            tenant.Settings);
    }
}
