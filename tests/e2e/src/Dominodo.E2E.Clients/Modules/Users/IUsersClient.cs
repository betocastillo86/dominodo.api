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
}
