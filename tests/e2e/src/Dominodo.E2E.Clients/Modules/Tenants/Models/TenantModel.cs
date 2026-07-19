namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/tenants</c>. Mirrors the API's <c>TenantDto</c>
/// by value. <c>Type</c>/<c>Status</c> are the corresponding enums serialized as strings.
/// </summary>
public sealed record TenantModel
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string City { get; init; } = default!;
}
