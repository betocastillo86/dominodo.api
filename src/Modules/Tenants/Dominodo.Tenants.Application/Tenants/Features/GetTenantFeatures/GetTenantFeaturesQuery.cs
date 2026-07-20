using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;

namespace Dominodo.Tenants.Application.Tenants.Features.GetTenantFeatures;

internal sealed record GetTenantFeaturesQuery(Guid TenantId) : IQuery<IReadOnlyList<TenantFeatureDto>>;

internal sealed class GetTenantFeaturesQueryHandler(ITenantRepository tenants)
    : IQueryHandler<GetTenantFeaturesQuery, IReadOnlyList<TenantFeatureDto>>
{
    public async Task<Result<IReadOnlyList<TenantFeatureDto>>> Handle(
        GetTenantFeaturesQuery query,
        CancellationToken ct)
    {
        var tenant = await tenants.GetByIdWithFeaturesAsync(query.TenantId, ct);
        if (tenant is null)
        {
            return Error.NotFound("Tenant.NotFound", "Tenant not found.");
        }

        IReadOnlyList<TenantFeatureDto> features = tenant.Features.Select(ToDto).ToList();
        return Result.Success(features);
    }

    private static TenantFeatureDto ToDto(TenantFeature f) => new(
        f.Id,
        f.TenantId,
        f.FeatureKey.ToString(),
        f.Enabled);
}
