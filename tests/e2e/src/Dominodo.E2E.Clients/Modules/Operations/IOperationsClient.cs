using Dominodo.E2E.Clients.Core.Models;
using Dominodo.E2E.Clients.Modules.Operations.Models;
using Refit;

namespace Dominodo.E2E.Clients.Modules.Operations;

/// <summary>
/// Refit client for the Operations module's Announcements HTTP surface. Hand-written, versioned routes.
/// Token flows via <c>[Authorize("Bearer")]</c>; null ⇒ anonymous request (how we test 401). Every
/// endpoint is tenant-scoped and REQUIRES the <c>X-Tenant</c> header (doc 09). The admin surface is
/// permission-gated (announcements.view / .create / .edit); <c>/mine</c> is auth-only.
/// </summary>
public interface IOperationsClient
{
    // AnnouncementsController.List, guarded by [HasPermission(Permissions.AnnouncementsView)] and scoped by
    // X-Tenant: anonymous ⇒ 401, authenticated without announcements.view ⇒ 403, with it ⇒ 200. Includes
    // drafts. No request validator (pagination clamped in PageRequest) — the only 400 is Tenant.Unknown.
    [Get("/api/v1/announcements")]
    Task<ApiResponse<PagedResultModel<AnnouncementModel>>> GetAnnouncements(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? status = null,
        [Query] string? category = null,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // AnnouncementsController.Mine, plain [Authorize] (no permission), scoped by X-Tenant. Returns the
    // active announcements relevant to the caller's audience (AllTenant ∪ matching ByTower/ByApartments).
    [Get("/api/v1/announcements/mine")]
    Task<ApiResponse<PagedResultModel<AnnouncementModel>>> GetMyAnnouncements(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? category = null,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // AnnouncementsController.GetById, guarded by [HasPermission(Permissions.AnnouncementsView)] and scoped
    // by X-Tenant. Unknown id (or one in another tenant) ⇒ 404 Announcement.NotFound.
    [Get("/api/v1/announcements/{id}")]
    Task<ApiResponse<AnnouncementDetailModel>> GetAnnouncementById(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // AnnouncementsController.Create, guarded by [HasPermission(Permissions.AnnouncementsCreate)] and scoped
    // by X-Tenant. Success is 201 Created ({ id }). Validation failures ⇒ 400 Validation.Failed.
    [Post("/api/v1/announcements")]
    Task<ApiResponse<CreatedModel>> CreateAnnouncement(
        [Body] NewAnnouncementModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // AnnouncementsController.Update, guarded by [HasPermission(Permissions.AnnouncementsEdit)] and scoped by
    // X-Tenant. Success is 204 NoContent. Unknown id ⇒ 404; an archived announcement ⇒ 409.
    [Put("/api/v1/announcements/{id}")]
    Task<ApiResponse<object>> UpdateAnnouncement(
        Guid id,
        [Body] UpdateAnnouncementModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // AnnouncementsController.Publish, guarded by [HasPermission(Permissions.AnnouncementsEdit)] and scoped
    // by X-Tenant. Success is 204 NoContent. Unknown id ⇒ 404; a non-draft ⇒ 409.
    [Put("/api/v1/announcements/{id}/publish")]
    Task<ApiResponse<object>> PublishAnnouncement(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // AnnouncementsController.Archive, guarded by [HasPermission(Permissions.AnnouncementsEdit)] and scoped
    // by X-Tenant. Success is 204 NoContent. Unknown id ⇒ 404; an already-archived announcement ⇒ 409.
    [Put("/api/v1/announcements/{id}/archive")]
    Task<ApiResponse<object>> ArchiveAnnouncement(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);
}
