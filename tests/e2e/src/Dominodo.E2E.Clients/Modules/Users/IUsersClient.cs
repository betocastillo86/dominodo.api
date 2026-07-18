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
}
