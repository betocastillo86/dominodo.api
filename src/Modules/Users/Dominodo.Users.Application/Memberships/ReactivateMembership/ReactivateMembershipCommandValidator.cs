using FluentValidation;

namespace Dominodo.Users.Application.Memberships.ReactivateMembership;

internal sealed class ReactivateMembershipCommandValidator : AbstractValidator<ReactivateMembershipCommand>
{
    public ReactivateMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
    }
}
