using Dominodo.Admin.Application.Notifications.GetNotificationTemplateById;
using Dominodo.Admin.Application.Notifications.GetNotificationTemplates;
using Dominodo.Admin.Application.Notifications.UpdateNotificationTemplate;
using Dominodo.Admin.Contracts;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Admin.Api.Controllers;

// Notification templates (domain-model §4.1). Catalog: view + edit only — no create-by-API (§4.2). Scope
// follows the X-Tenant header: present resolves the tenant override, absent the global default. Because
// Administrador holds notifications.* only through a tenant membership, an edit resolves only with
// X-Tenant, so it can only touch its own tenant's override; editing a global template needs a
// Platform-role token with no X-Tenant. No role-name check (doc 12).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/notification-templates")]
public sealed class NotificationTemplatesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.NotificationsView)]
    [EndpointSummary("Lists global default templates plus the current tenant's overrides (when X-Tenant is sent).")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new GetNotificationTemplatesQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetNotificationTemplateById")]
    [HasPermission(Permissions.NotificationsView)]
    [EndpointSummary("Gets a template by id. Global defaults are readable in any scope; a tenant override only within its tenant (leak-safe 404 otherwise).")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetNotificationTemplateByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.NotificationsEdit)]
    [EndpointSummary("Updates a template's content/channels. The row must belong to the current scope (tenant override with X-Tenant, else the global default).")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(Guid id, UpdateNotificationTemplateRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateNotificationTemplateCommand(
                id,
                request.Channels,
                request.EmailSubject,
                request.EmailBodyHtml,
                request.InAppText,
                request.PushText,
                request.IsActive,
                request.Localization),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record UpdateNotificationTemplateRequest(
    string Channels,
    string? EmailSubject,
    string? EmailBodyHtml,
    string? InAppText,
    string? PushText,
    bool IsActive,
    string? Localization);
