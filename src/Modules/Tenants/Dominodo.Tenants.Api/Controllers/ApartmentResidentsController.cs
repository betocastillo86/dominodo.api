using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Tenants.Application.Apartments.Residents.AssignResident;
using Dominodo.Tenants.Application.Apartments.Residents.EndResidency;
using Dominodo.Tenants.Application.Apartments.Residents.GetApartmentResidents;
using Dominodo.Tenants.Application.Apartments.Residents.RemoveResident;
using Dominodo.Tenants.Contracts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Tenants.Api.Controllers;

// Resident↔apartment links (domain-model §2.4). Tenant-scoped: every endpoint REQUIRES the X-Tenant
// header and is gated by tenants.manage (plan Phase 4). Residents are mutated through the Apartment
// aggregate, so all routes hang off an apartment.
[ApiController]
[HasPermission(Permissions.TenantsManage)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/apartments/{apartmentId:guid}/residents")]
public sealed class ApartmentResidentsController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetApartmentResidents")]
    [EndpointSummary("Lists the residents of an apartment (within the current tenant).")]
    [ProducesResponseType(typeof(IReadOnlyList<ResidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> List(Guid apartmentId, CancellationToken ct)
    {
        var result = await sender.Send(new GetApartmentResidentsQuery(apartmentId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [EndpointSummary("Assigns a resident (owner/renter) to an apartment. Requires the tenants.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Assign(Guid apartmentId, AssignResidentRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new AssignResidentCommand(
                apartmentId,
                request.UserId,
                request.RelationType,
                request.LivesHere,
                request.StartDate),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetApartmentResidents", new { apartmentId }, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpPut("{residentId:guid}/end")]
    [EndpointSummary("Ends an active residency (keeps history). Requires the tenants.manage permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> End(Guid apartmentId, Guid residentId, EndResidencyRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new EndResidencyCommand(apartmentId, residentId, request.EndDate), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpDelete("{residentId:guid}")]
    [EndpointSummary("Removes a residency row entirely. Requires the tenants.manage permission.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Remove(Guid apartmentId, Guid residentId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveResidentCommand(apartmentId, residentId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record AssignResidentRequest(
    Guid UserId,
    string RelationType,
    bool LivesHere,
    DateOnly? StartDate);

public sealed record EndResidencyRequest(DateOnly EndDate);
