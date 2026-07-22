namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record RequestStatusChangedIntegrationEvent(
    Guid RequestId,
    Guid TenantId,
    string FromStatus,
    string ToStatus,
    Guid ChangedByUserId);
