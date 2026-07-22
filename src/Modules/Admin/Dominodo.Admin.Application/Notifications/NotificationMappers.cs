using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Application.Notifications;

internal static class NotificationMappers
{
    public static InAppMessageDto ToDto(InAppMessage n) => new(
        n.Id, n.TenantId, n.RecipientUserId, n.Type.ToString(), n.Title, n.Body,
        n.TargetUrl, n.IsRead, n.ReadAtUtc, n.TriggeredByUserId, n.CreatedAtUtc);

    public static EmailMessageDto ToDto(EmailMessage m) => new(
        m.Id, m.TenantId, m.To, m.ToName, m.Subject, m.BodyHtml, m.Priority,
        m.Status.ToString(), m.Attempts, m.ScheduledAtUtc, m.SentAtUtc);

    public static PushMessageDto ToDto(PushMessage m) => new(
        m.Id, m.TenantId, m.RecipientUserId, m.Title, m.Body, m.TargetUrl,
        m.Platform.ToString(), m.Status.ToString(), m.Attempts, m.DedupHash, m.SentAtUtc);
}
