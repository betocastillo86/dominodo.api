using FluentValidation;

namespace Dominodo.Users.Application.Memberships.RemoveMembership;

internal sealed class RemoveMembershipCommandValidator : AbstractValidator<RemoveMembershipCommand>
{
    public RemoveMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
    }
}
