using Dominodo.Operations.Application.Announcements.ArchiveAnnouncement;
using Dominodo.Operations.Application.Announcements.CreateAnnouncement;
using Dominodo.Operations.Application.Announcements.GetAnnouncementById;
using Dominodo.Operations.Application.Announcements.GetAnnouncements;
using Dominodo.Operations.Application.Announcements.GetMyAnnouncements;
using Dominodo.Operations.Application.Announcements.PublishAnnouncement;
using Dominodo.Operations.Application.Announcements.UpdateAnnouncement;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Announcements;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Operations.Api.Controllers;

// Tenant-scoped announcements (comunicados). Every endpoint requires the X-Tenant header (doc 09). The
// admin surface is permission-gated (announcements.view / .create / .edit). GET /mine is auth-only: it
// returns the active announcements relevant to the caller's audience (no permission needed).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/announcements")]
public sealed class AnnouncementsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AnnouncementsView)]
    [EndpointSummary("Lists announcements in the current tenant (incl. drafts), paged and filterable.")]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AnnouncementStatus? status = null,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAnnouncementsQuery(page, pageSize, status, category), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("mine")]
    [Authorize]
    [EndpointSummary("Returns the active announcements relevant to the caller's audience, paged and optionally filtered by category. Auth-only (no permission).")]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<IResult> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyAnnouncementsQuery(page, pageSize, category), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetAnnouncementById")]
    [HasPermission(Permissions.AnnouncementsView)]
    [EndpointSummary("Gets an announcement by id (incl. drafts). Requires the announcements.view permission.")]
    [ProducesResponseType(typeof(AnnouncementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAnnouncementByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.AnnouncementsCreate)]
    [EndpointSummary("Creates an announcement draft. Requires the announcements.create permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Create(CreateAnnouncementRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateAnnouncementCommand(
                request.Title,
                request.Body,
                request.Priority,
                request.AudienceType,
                request.AudienceFilter,
                request.Category,
                request.ExpiresAtUtc),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetAnnouncementById", new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.AnnouncementsEdit)]
    [EndpointSummary("Edits an announcement. Requires the announcements.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, UpdateAnnouncementRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateAnnouncementCommand(
                id,
                request.Title,
                request.Body,
                request.Priority,
                request.AudienceType,
                request.AudienceFilter,
                request.Category,
                request.ExpiresAtUtc),
            ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/publish")]
    [HasPermission(Permissions.AnnouncementsEdit)]
    [EndpointSummary("Publishes a draft announcement. Requires the announcements.edit permission.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new PublishAnnouncementCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/archive")]
    [HasPermission(Permissions.AnnouncementsEdit)]
    [EndpointSummary("Archives an announcement. Requires the announcements.edit permission.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ArchiveAnnouncementCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record CreateAnnouncementRequest(
    string Title,
    string Body,
    byte Priority,
    AudienceType AudienceType,
    string? AudienceFilter,
    string? Category,
    DateTimeOffset? ExpiresAtUtc);

public sealed record UpdateAnnouncementRequest(
    string Title,
    string Body,
    byte Priority,
    AudienceType AudienceType,
    string? AudienceFilter,
    string? Category,
    DateTimeOffset? ExpiresAtUtc);
