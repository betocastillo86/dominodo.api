using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.ListNotifications;

// Admin read (notifications.view): in-app notifications for the current tenant. Requires an X-Tenant —
// these carry TenantId as a plain column and are listed by tenant, not via ForCurrentTenant (§4.2).
internal sealed record ListNotificationsQuery : IQuery<IReadOnlyList<UserNotificationDto>>;

internal sealed class ListNotificationsQueryHandler(
    IUserNotificationRepository notifications,
    ITenantContext tenant)
    : IQueryHandler<ListNotificationsQuery, IReadOnlyList<UserNotificationDto>>
{
    public async Task<Result<IReadOnlyList<UserNotificationDto>>> Handle(ListNotificationsQuery query, CancellationToken ct)
    {
        if (!tenant.HasTenant)
        {
            return Error.Validation("UserNotification.TenantRequired", "An X-Tenant header is required to list notifications.");
        }

        var rows = await notifications.GetForTenantAsync(tenant.TenantId, ct);
        return rows.Select(NotificationMappers.ToDto).ToList();
    }
}
