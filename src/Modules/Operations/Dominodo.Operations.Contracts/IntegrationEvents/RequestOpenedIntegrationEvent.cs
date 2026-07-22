namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record RequestOpenedIntegrationEvent(
    Guid RequestId,
    Guid TenantId,
    string Code,
    Guid CreatedByUserId,
    Guid? AssignedToUserId);
