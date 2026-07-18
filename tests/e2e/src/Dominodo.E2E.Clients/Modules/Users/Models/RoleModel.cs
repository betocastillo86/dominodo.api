namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/roles</c>. Mirrors the API's <c>RoleDto</c>
/// by value. <c>Scope</c> is the <c>RoleScope</c> enum as a string (e.g. "Platform", "Tenant").
/// </summary>
public sealed record RoleModel
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public string Scope { get; init; } = default!;
    public IReadOnlyList<int> PermissionIds { get; init; } = [];
}
