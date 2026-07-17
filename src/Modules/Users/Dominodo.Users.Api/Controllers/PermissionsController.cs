using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Users.Application.Permissions.GetPermissions;
using Dominodo.Users.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/permissions")]
public sealed class PermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists all permissions.")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new GetPermissionsQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
