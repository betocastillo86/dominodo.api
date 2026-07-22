using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Application.Notifications.GetNotificationTemplates;

// Lists global default templates plus, when an X-Tenant is resolved, that tenant's overrides (§4.1).
internal sealed record GetNotificationTemplatesQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<NotificationTemplateDto>>;

internal sealed class GetNotificationTemplatesQueryHandler(
    INotificationTemplateRepository templates,
    ITenantContext tenant)
    : IQueryHandler<GetNotificationTemplatesQuery, PagedResult<NotificationTemplateDto>>
{
    public async Task<Result<PagedResult<NotificationTemplateDto>>> Handle(GetNotificationTemplatesQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await templates.GetAllAsync(tenantId, page, ct);
        var dtos = items.Select(NotificationTemplateMapper.ToDto).ToList();
        return new PagedResult<NotificationTemplateDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
