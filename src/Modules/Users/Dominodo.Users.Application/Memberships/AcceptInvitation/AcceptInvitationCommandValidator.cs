using FluentValidation;

namespace Dominodo.Users.Application.Memberships.AcceptInvitation;

internal sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
