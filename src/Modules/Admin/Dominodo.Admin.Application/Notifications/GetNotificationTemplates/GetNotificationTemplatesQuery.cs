using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.GetNotificationTemplates;

// Lists global default templates plus, when an X-Tenant is resolved, that tenant's overrides (§4.1).
internal sealed record GetNotificationTemplatesQuery : IQuery<IReadOnlyList<NotificationTemplateDto>>;

internal sealed class GetNotificationTemplatesQueryHandler(
    INotificationTemplateRepository templates,
    ITenantContext tenant)
    : IQueryHandler<GetNotificationTemplatesQuery, IReadOnlyList<NotificationTemplateDto>>
{
    public async Task<Result<IReadOnlyList<NotificationTemplateDto>>> Handle(GetNotificationTemplatesQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var rows = await templates.GetAllAsync(tenantId, ct);
        return rows.Select(NotificationTemplateMapper.ToDto).ToList();
    }
}
