using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Notifications.ListEmailMessages;

// Admin read (notifications.view): email outbox artifacts, scoped to the current tenant when an X-Tenant
// is present, optionally filtered by status. Read-first artifacts (§4.2).
internal sealed record ListEmailMessagesQuery(string? Status = null) : IQuery<IReadOnlyList<EmailMessageDto>>;

internal sealed class ListEmailMessagesQueryHandler(
    IEmailMessageRepository messages,
    ITenantContext tenant)
    : IQueryHandler<ListEmailMessagesQuery, IReadOnlyList<EmailMessageDto>>
{
    public async Task<Result<IReadOnlyList<EmailMessageDto>>> Handle(ListEmailMessagesQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        DeliveryStatus? status = Enum.TryParse<DeliveryStatus>(query.Status, out var s) ? s : null;

        var rows = await messages.ListAsync(tenantId, status, ct);
        return rows.Select(NotificationMappers.ToDto).ToList();
    }
}
