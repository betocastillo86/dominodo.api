namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/apartments/{id}/status</c>. Mirrors the API's
/// <c>ChangeApartmentStatusRequest</c>. <c>Status</c> must parse to the <c>ApartmentStatus</c> enum
/// ("Occupied" or "Vacant").
/// </summary>
public sealed record ChangeApartmentStatusModel
{
    public string Status { get; init; } = default!;
}
