using FluentValidation;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.SendOtp;

internal sealed record SendOtpNotificationCommand(
    Guid EventId,
    string Phone,
    string? Email,
    string Code,
    string Purpose,
    bool HasWhatsApp) : ICommand;

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

internal sealed class SendOtpNotificationCommandHandler(
    IWhatsAppSender whatsApp,
    IEmailSender email,
    INotificationDeliveryRepository deliveries,
    IClock clock)
    : ICommandHandler<SendOtpNotificationCommand>
{
    public async Task<Result> Handle(SendOtpNotificationCommand command, CancellationToken ct)
    {
        // Idempotent by source event id — safe under at-least-once delivery (doc 11).
        if (await deliveries.ExistsForEventAsync(command.EventId, ct))
        {
            return Result.Success();
        }

        var body = $"Tu código de verificación de Dominodo es: {command.Code}";

        DeliveryChannel channel;
        string recipient;

        if (command.HasWhatsApp)
        {
            await whatsApp.SendAsync(new WhatsAppMessage(command.Phone, body), ct);
            channel = DeliveryChannel.WhatsApp;
            recipient = command.Phone;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.Email))
            {
                return Error.Validation("Otp.NoChannel", "No delivery channel available (no WhatsApp and no email).");
            }

            await email.SendAsync(
                new EmailMessage(command.Email, "Tu código de verificación", $"<p>{body}</p>", body),
                ct);
            channel = DeliveryChannel.Email;
            recipient = command.Email;
        }

        deliveries.Add(NotificationDelivery.Record(
            command.EventId, channel, recipient, command.Purpose, DeliveryStatus.Sent, clock));

        // No SaveChangesAsync — UnitOfWorkBehavior commits (persists the audit row + flushes outbox).
        return Result.Success();
    }
}
