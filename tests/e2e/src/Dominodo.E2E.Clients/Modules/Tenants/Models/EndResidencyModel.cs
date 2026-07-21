namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/apartments/{apartmentId}/residents/{residentId}/end</c>.
/// Mirrors the API's <c>EndResidencyRequest</c> by value.
/// </summary>
public sealed record EndResidencyModel
{
    public DateOnly EndDate { get; init; }
}
