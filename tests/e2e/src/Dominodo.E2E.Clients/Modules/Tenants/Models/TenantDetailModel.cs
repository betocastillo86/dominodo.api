namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated response body for <c>GET /api/v1/tenants/{id}</c>. Mirrors the API's
/// <c>TenantDetailDto</c> by value.
/// </summary>
public sealed record TenantDetailModel
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? LegalId { get; init; }
    public string Type { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Address { get; init; } = default!;
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
    public string? Branding { get; init; }
    public string? Settings { get; init; }
}
