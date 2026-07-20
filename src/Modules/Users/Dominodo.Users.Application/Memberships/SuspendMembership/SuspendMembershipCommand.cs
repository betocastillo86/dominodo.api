using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Memberships.SuspendMembership;

internal sealed record SuspendMembershipCommand(Guid MembershipId) : ICommand;
