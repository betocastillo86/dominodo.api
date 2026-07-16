using FluentValidation;

namespace Dominodo.Admin.Application.Notifications.SendOtp;

internal sealed class SendOtpNotificationCommandValidator : AbstractValidator<SendOtpNotificationCommand>
{
    public SendOtpNotificationCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty()
            .When(x => !x.HasWhatsApp)
            .WithMessage("Email is required when WhatsApp is not available.");
    }
}
