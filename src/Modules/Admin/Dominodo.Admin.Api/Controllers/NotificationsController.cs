using Dominodo.Admin.Application.Notifications.CreateInAppMessage;
using Dominodo.Admin.Application.Notifications.GetMyNotifications;
using Dominodo.Admin.Application.Notifications.ListNotifications;
using Dominodo.Admin.Application.Notifications.MarkNotificationRead;
using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Admin.Api.Controllers;

// In-app notifications (domain-model §4.2). Split authorization: the /me endpoints are self-service
// (any authenticated user reads/acks only their OWN rows — ownership, not RBAC — doc 12); the admin
// list/create endpoints require notifications.view / notifications.create.
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Lists the caller's own in-app notifications (newest first). Self-service — no notifications.* permission.")]
    [ProducesResponseType(typeof(PagedResult<InAppMessageDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyNotificationsQuery(unreadOnly, page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id:guid}/read")]
    [Authorize]
    [EndpointSummary("Marks one of the caller's own notifications read. Another user's notification is a leak-safe 404.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpGet]
    [HasPermission(Permissions.NotificationsView)]
    [EndpointSummary("Admin: lists in-app notifications for the current tenant (requires X-Tenant).")]
    [ProducesResponseType(typeof(PagedResult<InAppMessageDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new ListNotificationsQuery(page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.NotificationsCreate)]
    [EndpointSummary("Admin: materializes an in-app notification for a recipient in the current tenant (requires X-Tenant).")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Create(CreateInAppMessageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateInAppMessageCommand(
                request.RecipientUserId,
                request.Type,
                request.Title,
                request.Body,
                request.TargetUrl),
            ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/notifications/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }
}

public sealed record CreateInAppMessageRequest(
    Guid RecipientUserId,
    NotificationType Type,
    string Title,
    string Body,
    string? TargetUrl);
