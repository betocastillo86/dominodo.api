using FluentValidation;

namespace Dominodo.Users.Application.Memberships.SuspendMembership;

internal sealed class SuspendMembershipCommandValidator : AbstractValidator<SuspendMembershipCommand>
{
    public SuspendMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
    }
}
