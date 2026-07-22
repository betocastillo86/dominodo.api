using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.GetMyNotifications;

// Self-service (domain-model §4.2 / doc 12 ownership): returns ONLY the caller's own in-app
// notifications, keyed to ICurrentUser.UserId — not gated by a notifications.* permission.
internal sealed record GetMyNotificationsQuery(bool UnreadOnly = false) : IQuery<IReadOnlyList<UserNotificationDto>>;

internal sealed class GetMyNotificationsQueryHandler(
    IUserNotificationRepository notifications,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyNotificationsQuery, IReadOnlyList<UserNotificationDto>>
{
    public async Task<Result<IReadOnlyList<UserNotificationDto>>> Handle(GetMyNotificationsQuery query, CancellationToken ct)
    {
        var rows = await notifications.GetForRecipientAsync(currentUser.UserId, query.UnreadOnly, ct);
        return rows.Select(NotificationMappers.ToDto).ToList();
    }
}
