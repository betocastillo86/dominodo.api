using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Memberships.SuspendMembership;

internal sealed class SuspendMembershipCommandHandler(IMembershipRepository memberships)
    : ICommandHandler<SuspendMembershipCommand>
{
    public async Task<Result> Handle(SuspendMembershipCommand command, CancellationToken ct)
    {
        var membership = await memberships.GetByIdForCurrentTenantAsync(command.MembershipId, ct);
        if (membership is null)
        {
            return Error.NotFound("Membership.NotFound", "Membership not found in this conjunto.");
        }

        return membership.Suspend();
    }
}
