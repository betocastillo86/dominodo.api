using Dominodo.Operations.Application.Deliveries.GetDeliveries;
using Dominodo.Operations.Application.Deliveries.GetDeliveryById;
using Dominodo.Operations.Application.Deliveries.MarkDeliveryDelivered;
using Dominodo.Operations.Application.Deliveries.MarkDeliveryNotified;
using Dominodo.Operations.Application.Deliveries.MarkDeliveryReturned;
using Dominodo.Operations.Application.Deliveries.RegisterDelivery;
using Dominodo.Operations.Application.Deliveries.UpdateDelivery;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Operations.Api.Controllers;

// Tenant-scoped delivery (paquetería) management. Every endpoint requires the X-Tenant header (doc 09).
// Register needs deliveries.create; edit + status transitions need deliveries.edit; list needs
// deliveries.view. GET {id} is dual-mode: deliveries.view OR the caller is an active resident of the
// destination apartment (leak-safe 404 on denial).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/deliveries")]
public sealed class DeliveriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.DeliveriesView)]
    [EndpointSummary("Lists deliveries in the current tenant, paged and filterable.")]
    [ProducesResponseType(typeof(PagedResult<DeliveryDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DeliveryStatus? status = null,
        [FromQuery] DeliveryType? type = null,
        [FromQuery] Guid? apartmentId = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetDeliveriesQuery(page, pageSize, status, type, apartmentId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetDeliveryById")]
    [Authorize]
    [EndpointSummary("Gets a delivery by id. Dual-mode: deliveries.view reads any delivery; an active resident of the destination apartment reads their own. Any other id returns 404 (leak-safe).")]
    [ProducesResponseType(typeof(DeliveryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetDeliveryByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.DeliveriesCreate)]
    [EndpointSummary("Registers a delivery. Requires the deliveries.create permission (vigilante).")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DeliveryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Register(RegisterDeliveryRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterDeliveryCommand(
                request.ApartmentId,
                request.Type,
                request.Carrier,
                request.Comment,
                request.PhotoUrl,
                request.Metadata),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetDeliveryById", new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.DeliveriesEdit)]
    [EndpointSummary("Edits a delivery's details. Requires the deliveries.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, UpdateDeliveryRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateDeliveryCommand(id, request.Type, request.Carrier, request.Comment, request.PhotoUrl, request.Metadata),
            ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/notify")]
    [HasPermission(Permissions.DeliveriesEdit)]
    [EndpointSummary("Marks a delivery as notified (Received → Notified). Requires the deliveries.edit permission.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Notify(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkDeliveryNotifiedCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/deliver")]
    [HasPermission(Permissions.DeliveriesEdit)]
    [EndpointSummary("Marks a delivery as delivered. Requires the deliveries.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Deliver(Guid id, MarkDeliveryDeliveredRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new MarkDeliveryDeliveredCommand(id, request.ReceivedByName, request.DeliveredToUserId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/return")]
    [HasPermission(Permissions.DeliveriesEdit)]
    [EndpointSummary("Marks a delivery as returned. Requires the deliveries.edit permission.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Return(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkDeliveryReturnedCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record RegisterDeliveryRequest(
    Guid ApartmentId,
    DeliveryType Type,
    string? Carrier,
    string? Comment,
    string? PhotoUrl,
    string? Metadata);

public sealed record UpdateDeliveryRequest(
    DeliveryType Type,
    string? Carrier,
    string? Comment,
    string? PhotoUrl,
    string? Metadata);

public sealed record MarkDeliveryDeliveredRequest(string? ReceivedByName, Guid? DeliveredToUserId);
