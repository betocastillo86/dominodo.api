using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Memberships.ReactivateMembership;

internal sealed record ReactivateMembershipCommand(Guid MembershipId) : ICommand;
