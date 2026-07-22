using Dominodo.E2E.Clients.Core.Models;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Refit;

namespace Dominodo.E2E.Clients.Modules.Admin;

/// <summary>
/// Refit client for the Admin module's HTTP surface. Hand-written, versioned routes.
/// Token flows via <c>[Authorize("Bearer")]</c>; null ⇒ anonymous request.
/// </summary>
public interface IAdminClient
{
    // Register (or re-activate) a push device for the CURRENT user — DevicesController.Register, guarded by
    // plain [Authorize] (any valid bearer, ownership not RBAC — the device is keyed to the token's sub).
    // Anonymous ⇒ 401; invalid body ⇒ 400 Validation.Failed. Success is 201 Created ({"id": guid}).
    [Post("/api/v1/devices")]
    Task<ApiResponse<CreatedModel>> RegisterDevice(
        [Body] NewDeviceModel model,
        [Authorize("Bearer")] string? token = null);

    // Deactivate one of the CURRENT user's devices — DevicesController.Deactivate, guarded by plain
    // [Authorize]. No body; success is 204 NoContent. A device that does not exist OR belongs to another
    // user is a leak-safe 404 DeviceRegistration.NotFound (ownership check in the handler).
    [Delete("/api/v1/devices/{id}")]
    Task<IApiResponse> DeactivateDevice(
        Guid id,
        [Authorize("Bearer")] string? token = null);

    // Lists global default templates plus the current tenant's overrides (when X-Tenant is sent) —
    // NotificationTemplatesController.List, guarded by [HasPermission(Permissions.NotificationsView)].
    // Anonymous ⇒ 401; a valid bearer lacking the permission ⇒ 403. Success is 200 with a paged envelope.
    [Get("/api/v1/notification-templates")]
    Task<ApiResponse<PagedResultModel<NotificationTemplateModel>>> GetNotificationTemplates(
        [Query] int? page = null,
        [Query] int? pageSize = null,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Gets a template by id — NotificationTemplatesController.GetById, guarded by
    // [HasPermission(Permissions.NotificationsView)]. Global defaults are readable in any scope; a tenant
    // override only within its tenant (leak-safe 404 otherwise). Anonymous ⇒ 401; lacking permission ⇒ 403.
    [Get("/api/v1/notification-templates/{id}")]
    Task<ApiResponse<NotificationTemplateModel>> GetNotificationTemplateById(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Updates a template's content and per-channel toggles — NotificationTemplatesController.Update,
    // guarded by [HasPermission(Permissions.NotificationsEdit)]. The row must belong to the current scope
    // (tenant override with X-Tenant, else the global default). Anonymous ⇒ 401; lacking permission ⇒ 403;
    // invalid body ⇒ 400 Validation.Failed. Success is 204 NoContent.
    [Put("/api/v1/notification-templates/{id}")]
    Task<IApiResponse> UpdateNotificationTemplate(
        Guid id,
        [Body] UpdateNotificationTemplateModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Lists global settings plus the current tenant's overrides (when X-Tenant is sent) —
    // SystemSettingsController.List, guarded by [HasPermission(Permissions.SettingsView)]. Anonymous ⇒ 401;
    // a valid bearer lacking the permission ⇒ 403. Success is 200 with a paged envelope.
    [Get("/api/v1/system-settings")]
    Task<ApiResponse<PagedResultModel<SystemSettingModel>>> GetSystemSettings(
        [Query] int? page = null,
        [Query] int? pageSize = null,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Gets a setting resolved for the current scope (tenant override if present, else the global value) —
    // SystemSettingsController.GetByKey, guarded by [HasPermission(Permissions.SettingsView)]. Anonymous ⇒
    // 401; lacking permission ⇒ 403; an unknown key ⇒ 404 SystemSetting.NotFound. Success is 200.
    [Get("/api/v1/system-settings/{key}")]
    Task<ApiResponse<SystemSettingModel>> GetSystemSettingByKey(
        string key,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Creates a setting — SystemSettingsController.Create, guarded by [HasPermission(Permissions.SettingsCreate)].
    // With X-Tenant it creates a tenant override; without it a global row (Platform-role only). Anonymous ⇒
    // 401; lacking permission ⇒ 403; invalid body ⇒ 400 Validation.Failed; a duplicate (Key, scope) ⇒ 409
    // SystemSetting.AlreadyExists. Success is 201 Created ({"key": "..."}).
    [Post("/api/v1/system-settings")]
    Task<IApiResponse> CreateSystemSetting(
        [Body] NewSystemSettingModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Updates a setting's value for the current scope — SystemSettingsController.Update, guarded by
    // [HasPermission(Permissions.SettingsEdit)]. Anonymous ⇒ 401; lacking permission ⇒ 403; invalid body ⇒
    // 400 Validation.Failed; an unknown key ⇒ 404 SystemSetting.NotFound. Success is 204 NoContent.
    [Put("/api/v1/system-settings/{key}")]
    Task<IApiResponse> UpdateSystemSetting(
        string key,
        [Body] UpdateSystemSettingModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);
}
