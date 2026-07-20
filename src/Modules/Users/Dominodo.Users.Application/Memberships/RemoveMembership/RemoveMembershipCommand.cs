using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Memberships.RemoveMembership;

internal sealed record RemoveMembershipCommand(Guid MembershipId) : ICommand;
