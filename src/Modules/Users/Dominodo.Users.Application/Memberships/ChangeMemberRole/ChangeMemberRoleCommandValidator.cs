using FluentValidation;

namespace Dominodo.Users.Application.Memberships.ChangeMemberRole;

internal sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
