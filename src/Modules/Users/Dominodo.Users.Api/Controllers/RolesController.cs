using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Application.Roles;
using Dominodo.Users.Application.Roles.CreateRole;
using Dominodo.Users.Application.Roles.GetRoleById;
using Dominodo.Users.Application.Roles.GetRoles;
using Dominodo.Users.Application.Roles.UpdateRole;
using Dominodo.Users.Domain.Roles;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

[ApiController]
[HasPermission(Permissions.RolesManage)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists roles, paged.")]
    [ProducesResponseType(typeof(PagedResult<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] string? name = null,
        [FromQuery] RoleScope? scope = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRolesQuery(name, scope, page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:int}", Name = "GetRoleById")]
    [EndpointSummary("Gets a role by its identifier.")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(int id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoleByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [EndpointSummary("Creates a new role. Requires the roles.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateRoleCommand(
                request.Name,
                request.Description,
                request.Scope,
                request.PermissionIds ?? []),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetRoleById", new { id = result.Value }, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Updates an existing role. Requires the roles.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(int id, UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateRoleCommand(
                id,
                request.Name,
                request.Description,
                request.PermissionIds ?? []),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    RoleScope Scope,
    IReadOnlyList<int>? PermissionIds);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<int>? PermissionIds);
