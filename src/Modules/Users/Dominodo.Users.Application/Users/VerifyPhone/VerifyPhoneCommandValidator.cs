using FluentValidation;

namespace Dominodo.Users.Application.Users.VerifyPhone;

internal sealed class VerifyPhoneCommandValidator : AbstractValidator<VerifyPhoneCommand>
{
    public VerifyPhoneCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Phone must be in E.164 format (e.g. +573001234567).");

        RuleFor(x => x.Code).NotEmpty();
    }
}
