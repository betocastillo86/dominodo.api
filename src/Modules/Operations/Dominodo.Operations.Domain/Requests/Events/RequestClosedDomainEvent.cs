using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Requests.Events;

public sealed record RequestClosedDomainEvent(
    Guid RequestId,
    Guid TenantId,
    DateTimeOffset ClosedAtUtc) : IDomainEvent;
