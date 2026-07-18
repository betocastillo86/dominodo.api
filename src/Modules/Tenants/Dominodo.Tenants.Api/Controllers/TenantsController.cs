using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Application.Tenants.ChangeTenantStatus;
using Dominodo.Tenants.Application.Tenants.CreateTenant;
using Dominodo.Tenants.Application.Tenants.GetTenantById;
using Dominodo.Tenants.Application.Tenants.GetTenants;
using Dominodo.Tenants.Application.Tenants.UpdateTenant;
using Dominodo.Tenants.Contracts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Tenants.Api.Controllers;

// Platform-scoped tenant registry management. These operations precede any tenant, so they require
// NO X-Tenant header. Reads are gated by tenants.view, create by tenants.create, and other writes by
// tenants.edit (per-action, so the policies aren't AND-combined).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/tenants")]
public sealed class TenantsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.TenantsView)]
    [EndpointSummary("Lists tenants (conjuntos), paged.")]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTenantsQuery(page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetTenantById")]
    [HasPermission(Permissions.TenantsView)]
    [EndpointSummary("Gets a tenant by its identifier.")]
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.TenantsCreate)]
    [EndpointSummary("Creates a new tenant (conjunto). Requires the tenants.create permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(CreateTenantRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateTenantCommand(
                request.Slug,
                request.Name,
                request.Type,
                request.Address,
                request.City,
                request.Country,
                request.LegalId,
                request.Branding,
                request.Settings),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetTenantById", new { id = result.Value }, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.TenantsEdit)]
    [EndpointSummary("Updates a tenant's name and profile. Requires the tenants.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(Guid id, UpdateTenantRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateTenantCommand(
                id,
                request.Name,
                request.LegalId,
                request.Address,
                request.City,
                request.Country),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.TenantsEdit)]
    [EndpointSummary("Changes a tenant's status (activate/suspend/onboarding). Requires the tenants.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> ChangeStatus(Guid id, ChangeTenantStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeTenantStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record CreateTenantRequest(
    string Slug,
    string Name,
    string Type,
    string Address,
    string City,
    string Country,
    string? LegalId,
    string? Branding,
    string? Settings);

public sealed record UpdateTenantRequest(
    string Name,
    string? LegalId,
    string Address,
    string City,
    string Country);

public sealed record ChangeTenantStatusRequest(string Status);
