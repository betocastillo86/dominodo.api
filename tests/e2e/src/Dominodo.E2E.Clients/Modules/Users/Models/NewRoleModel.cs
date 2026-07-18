namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/roles</c>.
/// Mirrors the API's <c>CreateRoleRequest</c> by value.
/// <c>Scope</c> must be <c>"Platform"</c> or <c>"Tenant"</c>.
/// </summary>
public sealed record NewRoleModel
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Scope { get; init; } = default!;
    public IReadOnlyList<int>? PermissionIds { get; init; }
}
