namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/tenants/{tenantId}/features</c>. Mirrors the API's
/// <c>TenantFeatureDto</c> by value. <c>FeatureKey</c> is the <c>FeatureKey</c> enum as a string.
/// </summary>
public sealed record TenantFeatureModel
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string FeatureKey { get; init; } = default!;
    public bool Enabled { get; init; }
}
