using Dominodo.Admin.Domain.Notifications;

namespace Dominodo.Admin.Domain.Ports;

public interface IEmailMessageRepository
{
    void Add(EmailMessage message);

    // Admin read: email outbox artifacts, optionally filtered by tenant and/or status.
    Task<IReadOnlyList<EmailMessage>> ListAsync(
        Guid? tenantId,
        DeliveryStatus? status,
        CancellationToken cancellationToken = default);
}
