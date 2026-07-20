using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Memberships.AcceptInvitation;

internal sealed class AcceptInvitationCommandHandler(
    IMembershipRepository memberships,
    ITenantContext tenant,
    IClock clock)
    : ICommandHandler<AcceptInvitationCommand>
{
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken ct)
    {
        var membership = await memberships.GetByUserAndTenantAsync(command.UserId, tenant.TenantId, ct);
        if (membership is null)
        {
            return Error.NotFound("Membership.NotFound", "No membership to accept in this conjunto.");
        }

        return membership.Accept(clock.UtcNow);
    }
}
