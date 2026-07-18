using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Tenants.Application.Tenants.Features.GetTenantFeatures;

internal sealed record GetTenantFeaturesQuery(Guid TenantId) : IQuery<IReadOnlyList<TenantFeatureDto>>;
