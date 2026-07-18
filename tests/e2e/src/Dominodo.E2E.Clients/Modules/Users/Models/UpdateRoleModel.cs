namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/roles/{id}</c>.
/// Mirrors the API's <c>UpdateRoleRequest</c> by value.
/// </summary>
public sealed record UpdateRoleModel
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public IReadOnlyList<int>? PermissionIds { get; init; }
}
