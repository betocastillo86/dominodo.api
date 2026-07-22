using Dominodo.Admin.Application.Notifications.ListEmailMessages;
using Dominodo.Admin.Application.Notifications.ListPushMessages;
using Dominodo.Admin.Contracts;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Admin.Api.Controllers;

// Materialized message outbox artifacts (domain-model §4.2) — read-first. Admin only (notifications.view).
// Scoped to the current tenant when X-Tenant is present.
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/messages")]
public sealed class MessagesController(ISender sender) : ControllerBase
{
    [HttpGet("email")]
    [HasPermission(Permissions.NotificationsView)]
    [EndpointSummary("Lists email outbox messages, optionally filtered by status.")]
    [ProducesResponseType(typeof(IReadOnlyList<EmailMessageDto>), StatusCodes.Status200OK)]
    public async Task<IResult> ListEmail([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new ListEmailMessagesQuery(status), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("push")]
    [HasPermission(Permissions.NotificationsView)]
    [EndpointSummary("Lists push outbox messages, optionally filtered by status.")]
    [ProducesResponseType(typeof(IReadOnlyList<PushMessageDto>), StatusCodes.Status200OK)]
    public async Task<IResult> ListPush([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new ListPushMessagesQuery(status), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
