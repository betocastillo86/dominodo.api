using Dominodo.E2E.Clients.Core.Models;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Refit;

namespace Dominodo.E2E.Clients.Modules.Tenants;

/// <summary>
/// Refit client for the Tenants module's HTTP surface. Hand-written, versioned routes.
/// Token flows via <c>[Authorize("Bearer")]</c>; null ⇒ anonymous request (how we test 401).
/// All tenant registry endpoints are Platform-scoped — no <c>X-Tenant</c> header required.
/// </summary>
public interface ITenantsClient
{
    // Guarded by [HasPermission(Permissions.TenantsView)] on TenantsController:
    // anonymous ⇒ 401, authenticated without tenants.view ⇒ 403, with it ⇒ 200.
    [Get("/api/v1/tenants")]
    Task<ApiResponse<PagedResultModel<TenantModel>>> GetTenants(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/tenants/{id}")]
    Task<ApiResponse<TenantDetailModel>> GetTenantById(
        Guid id,
        [Authorize("Bearer")] string? token = null);

    // Guarded by [HasPermission(Permissions.TenantsCreate)]. Returns 201 with { id } (Guid).
    [Post("/api/v1/tenants")]
    Task<ApiResponse<CreatedModel>> CreateTenant(
        [Body] NewTenantModel model,
        [Authorize("Bearer")] string? token = null);

    // Guarded by [HasPermission(Permissions.TenantsEdit)]. Returns 204 NoContent on success.
    [Put("/api/v1/tenants/{id}")]
    Task<ApiResponse<object>> UpdateTenant(
        Guid id,
        [Body] UpdateTenantModel model,
        [Authorize("Bearer")] string? token = null);

    // Guarded by [HasPermission(Permissions.TenantsEdit)]. Returns 204 NoContent on success.
    [Put("/api/v1/tenants/{id}/status")]
    Task<ApiResponse<object>> ChangeTenantStatus(
        Guid id,
        [Body] ChangeTenantStatusModel model,
        [Authorize("Bearer")] string? token = null);

    // Guarded by [HasPermission(Permissions.TenantsView)] on TenantFeaturesController.
    [Get("/api/v1/tenants/{tenantId}/features")]
    Task<ApiResponse<IReadOnlyList<TenantFeatureModel>>> GetTenantFeatures(
        Guid tenantId,
        [Authorize("Bearer")] string? token = null);

    // Guarded by [HasPermission(Permissions.TenantsEdit)]. Returns 204 NoContent on success.
    [Put("/api/v1/tenants/{tenantId}/features/{featureKey}")]
    Task<ApiResponse<object>> SetTenantFeature(
        Guid tenantId,
        string featureKey,
        [Body] SetTenantFeatureModel model,
        [Authorize("Bearer")] string? token = null);
}
