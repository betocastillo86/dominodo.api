namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record RequestClosedIntegrationEvent(
    Guid RequestId,
    Guid TenantId,
    DateTimeOffset ClosedAtUtc);
