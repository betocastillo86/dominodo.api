using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Requests.Events;

public sealed record RequestOpenedDomainEvent(
    Guid RequestId,
    Guid TenantId,
    string Code,
    Guid CreatedByUserId,
    Guid? AssignedToUserId) : IDomainEvent;
