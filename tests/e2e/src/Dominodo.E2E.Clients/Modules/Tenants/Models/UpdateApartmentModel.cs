namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/apartments/{id}</c>. Mirrors the API's
/// <c>UpdateApartmentRequest</c> by value. <c>Type</c> is an <c>ApartmentType</c> name
/// ("Apartment", "House", "Commercial", "Parking", "Storage").
/// </summary>
public sealed record UpdateApartmentModel
{
    public string Number { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Tower { get; init; }
    public string? Attributes { get; init; }
}
