using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Requests.Events;

public sealed record RequestStatusChangedDomainEvent(
    Guid RequestId,
    Guid TenantId,
    RequestStatus FromStatus,
    RequestStatus ToStatus,
    Guid ChangedByUserId) : IDomainEvent;
