using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Users.Application.Roles.CreateRole;
using Dominodo.Users.Application.Roles.GetRoleById;
using Dominodo.Users.Application.Roles.GetRoles;
using Dominodo.Users.Application.Roles.UpdateRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Application.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRolesQuery(page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:int}", Name = "GetRoleById")]
    public async Task<IResult> GetById(int id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoleByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdmin")]
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
    [Authorize(Policy = "SuperAdmin")]
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
    string Scope,
    IReadOnlyList<int>? PermissionIds);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<int>? PermissionIds);
