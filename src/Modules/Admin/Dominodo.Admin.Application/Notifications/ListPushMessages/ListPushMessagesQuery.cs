using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Application.Notifications.ListPushMessages;

// Admin read (notifications.view): push outbox artifacts, scoped to the current tenant when an X-Tenant
// is present, optionally filtered by status. Read-first artifacts (§4.2).
internal sealed record ListPushMessagesQuery(string? Status = null, int Page = 1, int PageSize = 20) : IQuery<PagedResult<PushMessageDto>>;

internal sealed class ListPushMessagesQueryHandler(
    IPushMessageRepository messages,
    ITenantContext tenant)
    : IQueryHandler<ListPushMessagesQuery, PagedResult<PushMessageDto>>
{
    public async Task<Result<PagedResult<PushMessageDto>>> Handle(ListPushMessagesQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        DeliveryStatus? status = Enum.TryParse<DeliveryStatus>(query.Status, out var s) ? s : null;

        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await messages.ListAsync(tenantId, status, page, ct);
        var dtos = items.Select(NotificationMappers.ToDto).ToList();
        return new PagedResult<PushMessageDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
