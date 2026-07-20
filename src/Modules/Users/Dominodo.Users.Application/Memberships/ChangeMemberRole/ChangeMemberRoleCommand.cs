using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Memberships.ChangeMemberRole;

internal sealed record ChangeMemberRoleCommand(Guid MembershipId, int RoleId) : ICommand;
