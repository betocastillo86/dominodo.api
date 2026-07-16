using FluentValidation;

namespace Dominodo.Users.Application.Users.RequestPhoneVerification;

internal sealed class RequestPhoneVerificationCommandValidator : AbstractValidator<RequestPhoneVerificationCommand>
{
    public RequestPhoneVerificationCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Phone must be in E.164 format (e.g. +573001234567).");
    }
}
