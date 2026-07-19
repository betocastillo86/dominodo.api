namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/tenants</c>. Mirrors the API's
/// <c>CreateTenantRequest</c> by value. <c>Slug</c> must be lowercase kebab-case; <c>Type</c> must parse
/// to the <c>TenantType</c> enum ("Conjunto", "Edificio" or "Mixto").
/// </summary>
public sealed record NewTenantModel
{
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Address { get; init; } = default!;
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
    public string? LegalId { get; init; }
    public string? Branding { get; init; }
    public string? Settings { get; init; }
}
