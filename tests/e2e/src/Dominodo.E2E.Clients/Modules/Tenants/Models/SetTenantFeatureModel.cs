namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/tenants/{tenantId}/features/{featureKey}</c>. Mirrors
/// the API's <c>SetTenantFeatureRequest</c>. The feature key travels in the route; only <c>Enabled</c>
/// is in the body.
/// </summary>
public sealed record SetTenantFeatureModel
{
    public bool Enabled { get; init; }
}
