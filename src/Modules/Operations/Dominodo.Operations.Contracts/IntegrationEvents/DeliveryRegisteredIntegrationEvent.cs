namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record DeliveryRegisteredIntegrationEvent(
    Guid DeliveryId,
    Guid TenantId,
    string Code,
    Guid ApartmentId,
    Guid RegisteredByUserId);
