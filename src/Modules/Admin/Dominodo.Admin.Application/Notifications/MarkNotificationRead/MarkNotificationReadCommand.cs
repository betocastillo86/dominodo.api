using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.MarkNotificationRead;

// Self-service: marks the caller's own notification read. A notification owned by another user is a
// leak-safe 404 (ownership, not RBAC — doc 12).
internal sealed record MarkNotificationReadCommand(Guid Id) : ICommand;

internal sealed class MarkNotificationReadCommandHandler(
    IInAppMessageRepository notifications,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken ct)
    {
        var notification = await notifications.GetByIdAsync(command.Id, ct);
        if (notification is null || notification.RecipientUserId != currentUser.UserId)
        {
            return Error.NotFound("InAppMessage.NotFound", "No notification found for this id.");
        }

        notification.MarkRead(clock);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
