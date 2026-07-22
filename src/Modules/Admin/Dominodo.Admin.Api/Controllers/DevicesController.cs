using Dominodo.Admin.Application.Devices.DeactivateDevice;
using Dominodo.Admin.Application.Devices.RegisterDevice;
using Dominodo.Shared.Infrastructure.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Admin.Api.Controllers;

// Push-device registrations (domain-model §4.3). Fully self-service: any authenticated user manages only
// their OWN devices (keyed to ICurrentUser — ownership, not RBAC — doc 12). System-level, no tenant.
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/devices")]
public sealed class DevicesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [EndpointSummary("Registers (or re-activates) a push device for the current user. Idempotent by token.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Register(RegisterDeviceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterDeviceCommand(request.Platform, request.Token), ct);
        return result.IsSuccess
            ? Results.Created($"/api/v1/devices/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [EndpointSummary("Deactivates one of the current user's devices. Another user's device is a leak-safe 404.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeactivateDeviceCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record RegisterDeviceRequest(string Platform, string Token);
