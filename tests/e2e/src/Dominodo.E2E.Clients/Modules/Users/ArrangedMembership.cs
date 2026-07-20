using Dominodo.E2E.Clients.Modules.Users.Models;

namespace Dominodo.E2E.Clients.Modules.Users;

/// <summary>
/// Arrange result of <see cref="UsersRequestBuilder.InvitePlatformMembershipAsync"/>: the created
/// membership id (surfaced only by the invite endpoint), the invited user, and the granted role id.
/// Not an API DTO — a convenience carrier so tests can target the membership by id and mint the
/// invitee's own token.
/// </summary>
public sealed record ArrangedMembership(Guid MembershipId, UserModel User, int RoleId);
