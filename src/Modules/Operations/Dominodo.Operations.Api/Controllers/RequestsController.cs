using Dominodo.Operations.Application.Requests.AddRequestParticipant;
using Dominodo.Operations.Application.Requests.AddRequestUpdate;
using Dominodo.Operations.Application.Requests.AssignRequest;
using Dominodo.Operations.Application.Requests.ChangeRequestStatus;
using Dominodo.Operations.Application.Requests.DeleteRequest;
using Dominodo.Operations.Application.Requests.GetRequestById;
using Dominodo.Operations.Application.Requests.GetRequests;
using Dominodo.Operations.Application.Requests.OpenRequest;
using Dominodo.Operations.Application.Requests.UpdateRequest;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Operations.Api.Controllers;

// Tenant-scoped PQRS management. Every endpoint requires the X-Tenant header (doc 09). Opening a request
// and adding an update are member/participant actions ([Authorize], resolved in the handler); the rest
// are permission-gated (requests.view / requests.edit / requests.manage / requests.delete). GET {id} is
// dual-mode: requests.view OR the caller is a participant (leak-safe 404 on denial).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/requests")]
public sealed class RequestsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.RequestsView)]
    [EndpointSummary("Lists requests in the current tenant, paged and filterable.")]
    [ProducesResponseType(typeof(PagedResult<RequestDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] RequestType? type = null,
        [FromQuery] RequestPriority? priority = null,
        [FromQuery] Guid? assignedToUserId = null,
        [FromQuery] Guid? apartmentId = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetRequestsQuery(page, pageSize, status, type, priority, assignedToUserId, apartmentId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetRequestById")]
    [Authorize]
    [EndpointSummary("Gets a request by id. Dual-mode: requests.view reads any request; a participant reads their own. Any other id returns 404 (leak-safe).")]
    [ProducesResponseType(typeof(RequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRequestByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [Authorize]
    [EndpointSummary("Opens a PQRS request. Auth-only: requires an active membership in the resolved tenant (not a permission). The caller becomes the reporter.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Open(OpenRequestRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new OpenRequestCommand(
                request.Type,
                request.Title,
                request.Description,
                request.Priority,
                request.ApartmentId,
                request.Category,
                request.Location,
                request.Metadata),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetRequestById", new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.RequestsEdit)]
    [EndpointSummary("Edits request fields. Requires the requests.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, UpdateRequestRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateRequestCommand(
                id,
                request.Type,
                request.Title,
                request.Description,
                request.Priority,
                request.Category,
                request.Location,
                request.Metadata),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.RequestsManage)]
    [EndpointSummary("Changes a request's lifecycle status. Requires the requests.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> ChangeStatus(Guid id, ChangeRequestStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeRequestStatusCommand(id, request.Status, request.Note), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/assignee")]
    [HasPermission(Permissions.RequestsManage)]
    [EndpointSummary("Assigns a responsible collaborator. Requires the requests.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Assign(Guid id, AssignRequestRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AssignRequestCommand(id, request.AssignedToUserId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPost("{id:guid}/updates")]
    [Authorize]
    [EndpointSummary("Adds a timeline update. Dual-mode: requests.edit, OR the caller is a participant of this request.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> AddUpdate(Guid id, AddRequestUpdateRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddRequestUpdateCommand(id, request.Type, request.Body, request.IsInternal), ct);
        return result.IsSuccess
            ? Results.Created($"/api/v1/requests/{id}/updates/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    [HttpPost("{id:guid}/participants")]
    [HasPermission(Permissions.RequestsEdit)]
    [EndpointSummary("Adds a follower to a request. Requires the requests.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> AddParticipant(Guid id, AddRequestParticipantRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AddRequestParticipantCommand(id, request.UserId), ct);
        return result.IsSuccess
            ? Results.Created($"/api/v1/requests/{id}/participants/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.RequestsDelete)]
    [EndpointSummary("Deletes a request. Requires the requests.delete permission.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRequestCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record OpenRequestRequest(
    RequestType Type,
    string Title,
    string Description,
    RequestPriority Priority,
    Guid? ApartmentId,
    string? Category,
    string? Location,
    string? Metadata);

public sealed record UpdateRequestRequest(
    RequestType Type,
    string Title,
    string Description,
    RequestPriority Priority,
    string? Category,
    string? Location,
    string? Metadata);

public sealed record ChangeRequestStatusRequest(RequestStatus Status, string? Note);

public sealed record AssignRequestRequest(Guid AssignedToUserId);

public sealed record AddRequestUpdateRequest(RequestUpdateType Type, string? Body, bool IsInternal);

public sealed record AddRequestParticipantRequest(Guid UserId);
