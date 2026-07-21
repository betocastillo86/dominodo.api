namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/apartments</c>. Mirrors the API's <c>ApartmentDto</c>
/// by value. <c>Type</c>/<c>Status</c> are the corresponding enums serialized as strings.
/// </summary>
public sealed record ApartmentModel
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string? Tower { get; init; }
    public string Number { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Status { get; init; } = default!;
}
