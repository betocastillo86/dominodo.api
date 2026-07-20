using System.Security.Claims;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Application.Memberships.AcceptInvitation;
using Dominodo.Users.Application.Memberships.ChangeMemberRole;
using Dominodo.Users.Application.Memberships.GetTenantMemberships;
using Dominodo.Users.Application.Memberships.InviteMember;
using Dominodo.Users.Application.Memberships.ReactivateMembership;
using Dominodo.Users.Application.Memberships.RemoveMembership;
using Dominodo.Users.Application.Memberships.SuspendMembership;
using Dominodo.Users.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

// Tenant-scoped membership management. Every endpoint REQUIRES the X-Tenant header — the resolved tenant
// scopes all reads/writes (doc 09). Management is gated by memberships.manage (per-action). Accept is
// self-service: an invited user who lacks the permission can still accept their own invitation, so it is
// gated by plain [Authorize] instead.
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/memberships")]
public sealed class MembershipsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.MembershipsManage)]
    [EndpointSummary("Lists the current tenant's memberships, paged.")]
    [ProducesResponseType(typeof(PagedResult<MembershipDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTenantMembershipsQuery(page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("invite", Name = "InviteMember")]
    [HasPermission(Permissions.MembershipsManage)]
    [EndpointSummary("Invites a registered user (by phone) into the current conjunto. Requires memberships.manage.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Invite(InviteMemberRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new InviteMemberCommand(request.Phone, request.RoleId), ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("InviteMember", null, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpPost("accept")]
    [Authorize]
    [EndpointSummary("Accepts the caller's own invitation in the current conjunto (self-service).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Accept(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new AcceptInvitationCommand(userId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/role")]
    [HasPermission(Permissions.MembershipsManage)]
    [EndpointSummary("Changes a member's role. Requires memberships.manage.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> ChangeRole(Guid id, ChangeMemberRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeMemberRoleCommand(id, request.RoleId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/suspend")]
    [HasPermission(Permissions.MembershipsManage)]
    [EndpointSummary("Suspends a member. Requires memberships.manage.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Suspend(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new SuspendMembershipCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPut("{id:guid}/reactivate")]
    [HasPermission(Permissions.MembershipsManage)]
    [EndpointSummary("Reactivates a suspended member. Requires memberships.manage.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Reactivate(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ReactivateMembershipCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.MembershipsManage)]
    [EndpointSummary("Removes a member from the conjunto. Requires memberships.manage.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Remove(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveMembershipCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record InviteMemberRequest(string Phone, int RoleId);

public sealed record ChangeMemberRoleRequest(int RoleId);
