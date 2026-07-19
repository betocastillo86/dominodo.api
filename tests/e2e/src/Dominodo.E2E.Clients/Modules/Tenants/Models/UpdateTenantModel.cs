namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/tenants/{id}</c>. Mirrors the API's
/// <c>UpdateTenantRequest</c> by value (name + profile; slug and type are immutable via this endpoint).
/// </summary>
public sealed record UpdateTenantModel
{
    public string Name { get; init; } = default!;
    public string? LegalId { get; init; }
    public string Address { get; init; } = default!;
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
}
