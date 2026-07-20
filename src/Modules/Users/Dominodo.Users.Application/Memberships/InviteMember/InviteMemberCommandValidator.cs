using FluentValidation;

namespace Dominodo.Users.Application.Memberships.InviteMember;

internal sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Phone must be in E.164 format (e.g. +573001234567).");

        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
