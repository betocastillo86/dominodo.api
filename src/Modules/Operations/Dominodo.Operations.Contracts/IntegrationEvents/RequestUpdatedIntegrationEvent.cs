namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record RequestUpdatedIntegrationEvent(
    Guid RequestId,
    Guid TenantId,
    Guid UpdateId,
    Guid AuthorUserId);
