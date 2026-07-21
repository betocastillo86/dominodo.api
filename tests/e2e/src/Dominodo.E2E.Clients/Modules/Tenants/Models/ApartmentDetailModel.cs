namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated response for <c>GET /api/v1/apartments/{id}</c>. Mirrors the API's
/// <c>ApartmentDetailDto</c> by value (adds <c>Attributes</c> over the list item).
/// </summary>
public sealed record ApartmentDetailModel
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string? Tower { get; init; }
    public string Number { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? Attributes { get; init; }
}
