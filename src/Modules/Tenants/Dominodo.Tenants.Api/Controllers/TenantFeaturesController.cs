using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Tenants.Application.Tenants.Features.GetTenantFeatures;
using Dominodo.Tenants.Application.Tenants.Features.SetTenantFeature;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Tenants;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Tenants.Api.Controllers;

// Feature flags per conjunto (domain-model §2.2). Platform-scoped — managed by SuperAdmin, so NO
// X-Tenant header (the tenant is addressed by id in the route). Listing is gated by tenants.view,
// enabling/disabling by tenants.edit (per-action).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/tenants/{tenantId:guid}/features")]
public sealed class TenantFeaturesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.TenantsView)]
    [EndpointSummary("Lists a tenant's feature flags.")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantFeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> List(Guid tenantId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantFeaturesQuery(tenantId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{featureKey}")]
    [HasPermission(Permissions.TenantsEdit)]
    [EndpointSummary("Enables or disables a feature for a tenant (idempotent). Requires the tenants.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Set(Guid tenantId, FeatureKey featureKey, SetTenantFeatureRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetTenantFeatureCommand(tenantId, featureKey, request.Enabled), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record SetTenantFeatureRequest(bool Enabled);
