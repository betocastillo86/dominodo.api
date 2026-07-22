using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Application.Notifications;

internal static class NotificationTemplateMapper
{
    public static NotificationTemplateDto ToDto(NotificationTemplate t) => new(
        t.Id,
        t.TenantId,
        t.Type.ToString(),
        t.EmailEnabled,
        t.PushEnabled,
        t.InAppEnabled,
        t.EmailSubject,
        t.EmailBodyHtml,
        t.InAppText,
        t.PushText,
        t.IsActive,
        t.Localization);
}
