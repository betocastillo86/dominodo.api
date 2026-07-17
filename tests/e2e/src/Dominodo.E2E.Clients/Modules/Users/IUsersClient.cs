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

    // Authenticated endpoint used only for the JWT-factory smoke ([Authorize] on RolesController).
    // Full roles/permissions coverage is a later slice.
    [Get("/api/v1/roles")]
    Task<IApiResponse> GetRoles(
        [Authorize("Bearer")] string? token = null);
}
