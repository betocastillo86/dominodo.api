using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Application.Notifications.ListNotifications;

// Admin read (notifications.view): in-app notifications for the current tenant. Requires an X-Tenant —
// these carry TenantId as a plain column and are listed by tenant, not via ForCurrentTenant (§4.2).
internal sealed record ListNotificationsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<InAppMessageDto>>;

internal sealed class ListNotificationsQueryHandler(
    IInAppMessageRepository notifications,
    ITenantContext tenant)
    : IQueryHandler<ListNotificationsQuery, PagedResult<InAppMessageDto>>
{
    public async Task<Result<PagedResult<InAppMessageDto>>> Handle(ListNotificationsQuery query, CancellationToken ct)
    {
        if (!tenant.HasTenant)
        {
            return Error.Validation("InAppMessage.TenantRequired", "An X-Tenant header is required to list notifications.");
        }

        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await notifications.GetForTenantAsync(tenant.TenantId, page, ct);
        var dtos = items.Select(NotificationMappers.ToDto).ToList();
        return new PagedResult<InAppMessageDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
