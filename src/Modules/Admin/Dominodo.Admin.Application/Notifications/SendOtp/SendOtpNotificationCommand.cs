using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.SendOtp;

internal sealed record SendOtpNotificationCommand(
    Guid EventId,
    string Phone,
    string? Email,
    string Code,
    string Purpose,
    bool HasWhatsApp) : ICommand;
