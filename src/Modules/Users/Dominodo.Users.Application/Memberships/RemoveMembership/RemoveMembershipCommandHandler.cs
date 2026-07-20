using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Memberships.RemoveMembership;

// Hard-deletes the membership row (tenant-scoped load, so cross-tenant removal is impossible).
internal sealed class RemoveMembershipCommandHandler(IMembershipRepository memberships)
    : ICommandHandler<RemoveMembershipCommand>
{
    public async Task<Result> Handle(RemoveMembershipCommand command, CancellationToken ct)
    {
        var membership = await memberships.GetByIdForCurrentTenantAsync(command.MembershipId, ct);
        if (membership is null)
        {
            return Error.NotFound("Membership.NotFound", "Membership not found in this conjunto.");
        }

        memberships.Remove(membership);
        return Result.Success();
    }
}
