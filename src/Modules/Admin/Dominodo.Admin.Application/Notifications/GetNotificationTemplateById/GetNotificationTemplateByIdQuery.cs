using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.GetNotificationTemplateById;

internal sealed record GetNotificationTemplateByIdQuery(Guid Id) : IQuery<NotificationTemplateDto>;

// Global defaults (TenantId null) are readable in any scope; a tenant override is visible only within
// its own tenant. Any other id is a leak-safe 404.
internal sealed class GetNotificationTemplateByIdQueryHandler(
    INotificationTemplateRepository templates,
    ITenantContext tenant)
    : IQueryHandler<GetNotificationTemplateByIdQuery, NotificationTemplateDto>
{
    public async Task<Result<NotificationTemplateDto>> Handle(GetNotificationTemplateByIdQuery query, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(query.Id, ct);

        Guid? scopeTenantId = tenant.HasTenant ? tenant.TenantId : null;
        if (template is null || (template.TenantId is not null && template.TenantId != scopeTenantId))
        {
            return Error.NotFound("NotificationTemplate.NotFound", "No notification template found for this id.");
        }

        return NotificationTemplateMapper.ToDto(template);
    }
}
