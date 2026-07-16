using Dominodo.Admin.Application.Notifications.SendOtp;
using Dominodo.Users.Contracts.IntegrationEvents;
using MediatR;

namespace Dominodo.Admin.Application.Consumers;

// Inbound adapter (public, like a controller): Wolverine discovers it by convention (class ends in
// "Handler", method named "Handle", first parameter is the message). It only dispatches THIS module's
// own MediatR command — it cannot reach another module's internal requests, so the boundary holds.
// Dependencies are injected as METHOD parameters (Wolverine's sanctioned pattern — constructor
// injection here trips ServiceLocationPolicy.NotAllowed). Public because Wolverine's generated code
// cannot access internal types. Idempotent per ADR-0007.
public sealed class UserOtpRequestedHandler
{
    public Task Handle(UserOtpRequestedIntegrationEvent message, ISender sender, CancellationToken ct) =>
        sender.Send(
            new SendOtpNotificationCommand(
                message.EventId,
                message.Phone,
                message.Email,
                message.Code,
                message.Purpose,
                message.HasWhatsApp),
            ct);
}
