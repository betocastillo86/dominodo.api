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
}
