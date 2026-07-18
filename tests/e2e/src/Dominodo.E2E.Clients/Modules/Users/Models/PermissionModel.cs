namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/permissions</c>.
/// Mirrors the API's <c>PermissionDto</c> by value.
/// </summary>
public sealed record PermissionModel
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Group { get; init; } = default!;
}
