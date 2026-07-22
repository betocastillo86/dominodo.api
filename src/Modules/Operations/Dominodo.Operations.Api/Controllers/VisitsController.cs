using Dominodo.Operations.Application.Visits.FinishVisit;
using Dominodo.Operations.Application.Visits.GetVisitById;
using Dominodo.Operations.Application.Visits.GetVisits;
using Dominodo.Operations.Application.Visits.RegisterVisit;
using Dominodo.Operations.Application.Visits.UpdateVisit;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Operations.Api.Controllers;

// Tenant-scoped visit (visitas) management. Every endpoint requires the X-Tenant header (doc 09).
// Register needs visits.create; edit + finish need visits.edit; list needs visits.view. GET {id} is
// dual-mode: visits.view OR the caller is an active resident of the destination apartment (leak-safe 404).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/visits")]
public sealed class VisitsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.VisitsView)]
    [EndpointSummary("Lists visits in the current tenant, paged and filterable.")]
    [ProducesResponseType(typeof(PagedResult<VisitDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] VisitStatus? status = null,
        [FromQuery] VisitType? type = null,
        [FromQuery] Guid? apartmentId = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetVisitsQuery(page, pageSize, status, type, apartmentId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetVisitById")]
    [Authorize]
    [EndpointSummary("Gets a visit by id. Dual-mode: visits.view reads any visit; an active resident of the destination apartment reads their own. Any other id returns 404 (leak-safe).")]
    [ProducesResponseType(typeof(VisitDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetVisitByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.VisitsCreate)]
    [EndpointSummary("Registers a visit. Requires the visits.create permission (vigilante).")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(VisitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Register(RegisterVisitRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterVisitCommand(
                request.ApartmentId,
                request.Type,
                request.VisitorName,
                request.VisitorDocument,
                request.PhotoUrl,
                request.VehiclePlate,
                request.AuthorizedByUserId,
                request.Metadata),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetVisitById", new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.VisitsEdit)]
    [EndpointSummary("Edits a visit's details. Requires the visits.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, UpdateVisitRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateVisitCommand(
                id,
                request.Type,
                request.VisitorName,
                request.VisitorDocument,
                request.PhotoUrl,
                request.VehiclePlate,
                request.Metadata),
            ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/finish")]
    [HasPermission(Permissions.VisitsEdit)]
    [EndpointSummary("Finishes a visit (InProgress → Finished). Requires the visits.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Finish(Guid id, FinishVisitRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new FinishVisitCommand(id, request.AmountPaid), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record RegisterVisitRequest(
    Guid ApartmentId,
    VisitType Type,
    string VisitorName,
    string? VisitorDocument,
    string? PhotoUrl,
    string? VehiclePlate,
    Guid? AuthorizedByUserId,
    string? Metadata);

public sealed record UpdateVisitRequest(
    VisitType Type,
    string VisitorName,
    string? VisitorDocument,
    string? PhotoUrl,
    string? VehiclePlate,
    string? Metadata);

public sealed record FinishVisitRequest(decimal? AmountPaid);
