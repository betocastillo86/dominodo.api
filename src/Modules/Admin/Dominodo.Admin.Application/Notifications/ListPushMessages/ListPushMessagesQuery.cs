using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.ListPushMessages;

// Admin read (notifications.view): push outbox artifacts, scoped to the current tenant when an X-Tenant
// is present, optionally filtered by status. Read-first artifacts (§4.2).
internal sealed record ListPushMessagesQuery(string? Status = null) : IQuery<IReadOnlyList<PushMessageDto>>;

internal sealed class ListPushMessagesQueryHandler(
    IPushMessageRepository messages,
    ITenantContext tenant)
    : IQueryHandler<ListPushMessagesQuery, IReadOnlyList<PushMessageDto>>
{
    public async Task<Result<IReadOnlyList<PushMessageDto>>> Handle(ListPushMessagesQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        DeliveryStatus? status = Enum.TryParse<DeliveryStatus>(query.Status, out var s) ? s : null;

        var rows = await messages.ListAsync(tenantId, status, ct);
        return rows.Select(NotificationMappers.ToDto).ToList();
    }
}
