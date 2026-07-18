using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Application.Apartments.ChangeApartmentStatus;
using Dominodo.Tenants.Application.Apartments.CreateApartment;
using Dominodo.Tenants.Application.Apartments.GetApartmentById;
using Dominodo.Tenants.Application.Apartments.GetApartments;
using Dominodo.Tenants.Application.Apartments.UpdateApartment;
using Dominodo.Tenants.Contracts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Tenants.Api.Controllers;

// Tenant-scoped apartment management. Every endpoint REQUIRES the X-Tenant header — the resolved tenant
// scopes all reads/writes (doc 09). Apartment administration is part of managing a conjunto, so all
// operations are gated by tenants.manage (plan Phase 3).
[ApiController]
[HasPermission(Permissions.TenantsManage)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/apartments")]
public sealed class ApartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists apartments in the current tenant, paged and filterable.")]
    [ProducesResponseType(typeof(PagedResult<ApartmentDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? tower = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetApartmentsQuery(page, pageSize, tower, type, status), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetApartmentById")]
    [EndpointSummary("Gets an apartment by its identifier (within the current tenant).")]
    [ProducesResponseType(typeof(ApartmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetApartmentByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [EndpointSummary("Creates an apartment in the current tenant. Requires the tenants.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(CreateApartmentRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateApartmentCommand(
                request.Number,
                request.Type,
                request.Tower,
                request.Attributes),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetApartmentById", new { id = result.Value }, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Updates an apartment. Requires the tenants.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, UpdateApartmentRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateApartmentCommand(
                id,
                request.Number,
                request.Type,
                request.Tower,
                request.Attributes),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/status")]
    [EndpointSummary("Changes an apartment's status (occupied/vacant). Requires the tenants.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> ChangeStatus(Guid id, ChangeApartmentStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeApartmentStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record CreateApartmentRequest(
    string Number,
    string Type,
    string? Tower,
    string? Attributes);

public sealed record UpdateApartmentRequest(
    string Number,
    string Type,
    string? Tower,
    string? Attributes);

public sealed record ChangeApartmentStatusRequest(string Status);
