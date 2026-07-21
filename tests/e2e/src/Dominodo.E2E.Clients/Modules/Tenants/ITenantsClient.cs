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

    // Apartments (tenant-scoped) — ApartmentsController.List. The endpoint REQUIRES the X-Tenant header;
    // the resolved tenant scopes the read. Guarded by [HasPermission(Permissions.ApartmentsView)]:
    // anonymous ⇒ 401, authenticated without apartments.view (for the resolved tenant) ⇒ 403, with it ⇒ 200.
    [Get("/api/v1/apartments")]
    Task<ApiResponse<PagedResultModel<ApartmentModel>>> GetApartments(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? tower = null,
        [Query] string? type = null,
        [Query] string? status = null,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // ApartmentsController.Create, guarded by [HasPermission(Permissions.ApartmentsCreate)] and scoped by
    // X-Tenant. Success is 201 Created ({ id }). Duplicate (tower, number) in the tenant ⇒ 409.
    [Post("/api/v1/apartments")]
    Task<ApiResponse<CreatedModel>> CreateApartment(
        [Body] NewApartmentModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // ApartmentsController.GetById, [Authorize] dual-mode (apartments.view OR active resident), scoped by
    // X-Tenant. Any id not visible to the caller in the resolved tenant ⇒ 404 (leak-safe).
    [Get("/api/v1/apartments/{id}")]
    Task<ApiResponse<ApartmentDetailModel>> GetApartmentById(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // ApartmentsController.Update, guarded by [HasPermission(Permissions.ApartmentsEdit)] and scoped by
    // X-Tenant. Success is 204 NoContent. Unknown id (or one in another tenant) ⇒ 404; a colliding
    // (Tower, Number) in the tenant ⇒ 409.
    [Put("/api/v1/apartments/{id}")]
    Task<ApiResponse<object>> UpdateApartment(
        Guid id,
        [Body] UpdateApartmentModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // ApartmentsController.ChangeStatus, guarded by [HasPermission(Permissions.ApartmentsEdit)] and scoped by
    // X-Tenant. Success is 204 NoContent. Unknown id ⇒ 404; setting the status it already has ⇒ 409
    // (Apartment.AlreadyOccupied / Apartment.AlreadyVacant).
    [Put("/api/v1/apartments/{id}/status")]
    Task<ApiResponse<object>> ChangeApartmentStatus(
        Guid id,
        [Body] ChangeApartmentStatusModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // ApartmentResidentsController.Assign, guarded by [HasPermission(Permissions.TenantsEdit)] and scoped by
    // X-Tenant. Success is 201 Created ({ id }). The referenced user must exist in Users (cross-module facade).
    [Post("/api/v1/apartments/{apartmentId}/residents")]
    Task<ApiResponse<CreatedModel>> AssignResident(
        Guid apartmentId,
        [Body] AssignResidentModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);
}
