using Dominodo.E2E.Clients.Core.Models;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Refit;

namespace Dominodo.E2E.Clients.Modules.Users;

/// <summary>
/// Refit client for the Users module's HTTP surface. Hand-written, versioned routes.
/// Token flows via <c>[Authorize("Bearer")]</c>; null ⇒ anonymous request.
/// </summary>
public interface IUsersClient
{
    [Post("/api/v1/users")]
    Task<ApiResponse<CreatedModel>> Register(
        [Body] NewUserModel model,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/users/{id}")]
    Task<ApiResponse<UserModel>> GetById(
        Guid id,
        [Authorize("Bearer")] string? token = null);

    // Admin listing — UsersController.List, guarded by [HasPermission(Permissions.UsersView)]:
    // anonymous ⇒ 401, authenticated without users.view ⇒ 403, with it ⇒ 200. All filters optional.
    // page/pageSize are CLAMPED server-side (no validator), so an out-of-range page is NOT a 400 —
    // the only 400 is a model-binding failure (e.g. a non-enum `status`), hence status is a string
    // on the wire so a test can send an unparseable value.
    [Get("/api/v1/users")]
    Task<ApiResponse<PagedResultModel<UserListItemModel>>> GetUsers(
        [Query] Guid? tenantId = null,
        [Query] string? name = null,
        [Query] string? email = null,
        [Query] string? phone = null,
        [Query] string? status = null,
        [Query] string? documentNumber = null,
        [Query] bool? phoneVerified = null,
        [Query] bool? emailVerified = null,
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Authorize("Bearer")] string? token = null);

    // Guarded by [HasPermission(Permissions.RolesManage)] on RolesController: anonymous ⇒ 401,
    // authenticated without roles.manage ⇒ 403, SuperAdmin (or a user with the permission) ⇒ 200.
    [Get("/api/v1/roles")]
    Task<ApiResponse<PagedResultModel<RoleModel>>> GetRoles(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/roles/{id}")]
    Task<ApiResponse<RoleDetailModel>> GetRoleById(
        int id,
        [Authorize("Bearer")] string? token = null);

    [Post("/api/v1/roles")]
    Task<ApiResponse<CreatedIntModel>> CreateRole(
        [Body] NewRoleModel model,
        [Authorize("Bearer")] string? token = null);

    [Put("/api/v1/roles/{id}")]
    Task<ApiResponse<object>> UpdateRole(
        int id,
        [Body] UpdateRoleModel model,
        [Authorize("Bearer")] string? token = null);

    // OTP verification — both anonymous, available on any registered phone.
    [Post("/api/v1/auth/verify/request")]
    Task<IApiResponse> RequestOtp(
        [Body] RequestOtpModel model);

    [Post("/api/v1/auth/verify/confirm")]
    Task<IApiResponse> ConfirmOtp(
        [Body] ConfirmOtpModel model);

    // Anonymous — no token needed. Returns 400 on validation failure, 401 on invalid credentials.
    [Post("/api/v1/auth/login")]
    Task<ApiResponse<AuthTokensModel>> Login(
        [Body] LoginModel model);

    // Guarded by [HasPermission(Permissions.RolesManage)] on PermissionsController.
    [Get("/api/v1/permissions")]
    Task<ApiResponse<IReadOnlyList<PermissionModel>>> GetPermissions(
        [Authorize("Bearer")] string? token = null);

    // Memberships (tenant-scoped) — MembershipsController.List. The explicit [Header("X-Tenant")] param
    // controls the resolved tenant per call (a null value omits the header); it takes precedence over the
    // ambient TenantHeaderHandler.
    //
    // Guarded by [HasPermission(Permissions.MembershipsManage)]: anonymous ⇒ 401, authenticated without the
    // permission (resolved for the tenant) ⇒ 403, with it (platform grant or Active membership) ⇒ 200.
    [Get("/api/v1/memberships")]
    Task<ApiResponse<PagedResultModel<MembershipModel>>> GetMemberships(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Invite a registered user (by phone) into the resolved tenant — MembershipsController.Invite,
    // guarded by [HasPermission(Permissions.MembershipsManage)] and scoped by the X-Tenant header.
    // Success is 201 Created ({"id": guid}). The X-Tenant param is resolved BEFORE authorization by
    // TenantResolutionMiddleware: an unknown slug ⇒ 400 Tenant.Unknown; a valid slug the tenant-scoped
    // caller has no Active membership in ⇒ authorization fails closed ⇒ 403 (tenant isolation).
    [Post("/api/v1/memberships/invite")]
    Task<ApiResponse<CreatedModel>> InviteMember(
        [Body] InviteMemberModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Accept the caller's OWN invitation in the resolved tenant — MembershipsController.Accept. Self-service,
    // so guarded by plain [Authorize] (any valid bearer, no permission). No body; success is 204 NoContent.
    // Scoped by X-Tenant (resolved before the handler): unknown slug ⇒ 400 Tenant.Unknown. The handler looks
    // up the membership by (caller sub, resolved tenant): none ⇒ 404 Membership.NotFound; a non-Invited
    // membership (e.g. already Active) ⇒ 409 Membership.NotInvited.
    [Post("/api/v1/memberships/accept")]
    Task<IApiResponse> AcceptInvitation(
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Change a member's role — MembershipsController.ChangeRole, guarded by
    // [HasPermission(Permissions.MembershipsManage)] and scoped by X-Tenant. Success is 204 NoContent.
    // The membership is loaded ForCurrentTenant, so an unknown id (or one in another tenant) ⇒ 404
    // Membership.NotFound; a non-existent role ⇒ 400 Membership.RoleNotFound; a Platform-scope role ⇒
    // 400 Membership.RoleNotTenantScoped.
    [Put("/api/v1/memberships/{id}/role")]
    Task<IApiResponse> ChangeMemberRole(
        Guid id,
        [Body] ChangeMemberRoleModel model,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Suspend a member — MembershipsController.Suspend, guarded by
    // [HasPermission(Permissions.MembershipsManage)] and scoped by X-Tenant. No body; success is 204
    // NoContent. The membership is loaded ForCurrentTenant: unknown id (or one in another tenant) ⇒ 404
    // Membership.NotFound; a non-Active membership (e.g. still Invited) ⇒ 409 Membership.NotActive.
    [Put("/api/v1/memberships/{id}/suspend")]
    Task<IApiResponse> SuspendMembership(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Reactivate a suspended member — MembershipsController.Reactivate, guarded by
    // [HasPermission(Permissions.MembershipsManage)] and scoped by X-Tenant. No body; success is 204
    // NoContent. The membership is loaded ForCurrentTenant: unknown id (or one in another tenant) ⇒ 404
    // Membership.NotFound; a non-Suspended membership (e.g. Active) ⇒ 409 Membership.NotSuspended.
    [Put("/api/v1/memberships/{id}/reactivate")]
    Task<IApiResponse> ReactivateMembership(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);

    // Remove a member from the conjunto — MembershipsController.Remove, guarded by
    // [HasPermission(Permissions.MembershipsManage)] and scoped by X-Tenant. Hard-deletes the row (any
    // status); success is 204 NoContent. The membership is loaded ForCurrentTenant: unknown id (or one in
    // another tenant) ⇒ 404 Membership.NotFound.
    [Delete("/api/v1/memberships/{id}")]
    Task<IApiResponse> RemoveMembership(
        Guid id,
        [Header("X-Tenant")] string? tenant = null,
        [Authorize("Bearer")] string? token = null);
}
