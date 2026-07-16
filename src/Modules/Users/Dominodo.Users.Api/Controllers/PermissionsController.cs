using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Users.Application.Permissions.GetPermissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/permissions")]
public sealed class PermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new GetPermissionsQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
