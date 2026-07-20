namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/memberships</c>. Mirrors the API's
/// <c>MembershipDto</c> by value. <c>Status</c> is the <c>MembershipStatus</c> enum as a string
/// (e.g. "Invited", "Active", "Suspended").
/// </summary>
public sealed record MembershipModel
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public int RoleId { get; init; }
    public string RoleName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTimeOffset? InvitedAtUtc { get; init; }
    public DateTimeOffset? JoinedAtUtc { get; init; }
}
