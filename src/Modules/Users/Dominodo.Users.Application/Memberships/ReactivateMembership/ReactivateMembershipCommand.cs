using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;
using FluentValidation;

namespace Dominodo.Users.Application.Memberships.ReactivateMembership;

internal sealed record ReactivateMembershipCommand(Guid MembershipId) : ICommand;

internal sealed class ReactivateMembershipCommandValidator : AbstractValidator<ReactivateMembershipCommand>
{
    public ReactivateMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
    }
}

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
