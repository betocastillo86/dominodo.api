using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;
using FluentValidation;

namespace Dominodo.Users.Application.Memberships.SuspendMembership;

internal sealed record SuspendMembershipCommand(Guid MembershipId) : ICommand;

internal sealed class SuspendMembershipCommandValidator : AbstractValidator<SuspendMembershipCommand>
{
    public SuspendMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
    }
}

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
