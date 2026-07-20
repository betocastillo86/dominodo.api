namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/memberships/invite</c>.
/// Mirrors the API's <c>InviteMemberRequest</c> by value: the registered user's E.164
/// <c>Phone</c> and the Tenant-scope <c>RoleId</c> to grant in the current conjunto.
/// </summary>
public sealed record InviteMemberModel
{
    public string Phone { get; init; } = default!;
    public int RoleId { get; init; }
}
