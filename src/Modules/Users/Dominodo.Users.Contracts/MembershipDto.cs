namespace Dominodo.Users.Contracts;

// Public projection of a Membership (domain-model §1.6). Shared by the tenant controller's list and the
// Users facade (GetMemberships). Keep Contracts thin (Shared.Kernel only).
public sealed record MembershipDto(
    Guid UserId,
    Guid TenantId,
    int RoleId,
    string RoleName,
    string Status,
    DateTimeOffset? InvitedAtUtc,
    DateTimeOffset? JoinedAtUtc);
