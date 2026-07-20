namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/memberships/{id}/role</c>.
/// Mirrors the API's <c>ChangeMemberRoleRequest</c> by value: the Tenant-scope <c>RoleId</c>
/// to grant the member in the current conjunto.
/// </summary>
public sealed record ChangeMemberRoleModel
{
    public int RoleId { get; init; }
}
