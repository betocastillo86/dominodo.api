using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Requests.Events;

public sealed record RequestUpdatedDomainEvent(
    Guid RequestId,
    Guid TenantId,
    Guid UpdateId,
    Guid AuthorUserId) : IDomainEvent;
