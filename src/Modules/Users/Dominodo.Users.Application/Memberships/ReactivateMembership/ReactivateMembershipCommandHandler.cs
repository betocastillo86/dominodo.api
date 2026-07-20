using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Memberships.ReactivateMembership;

internal sealed class ReactivateMembershipCommandHandler(IMembershipRepository memberships)
    : ICommandHandler<ReactivateMembershipCommand>
{
    public async Task<Result> Handle(ReactivateMembershipCommand command, CancellationToken ct)
    {
        var membership = await memberships.GetByIdForCurrentTenantAsync(command.MembershipId, ct);
        if (membership is null)
        {
            return Error.NotFound("Membership.NotFound", "Membership not found in this conjunto.");
        }

        return membership.Reactivate();
    }
}
