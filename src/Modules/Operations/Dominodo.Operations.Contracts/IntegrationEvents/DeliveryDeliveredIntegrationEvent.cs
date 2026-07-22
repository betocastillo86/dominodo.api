namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record DeliveryDeliveredIntegrationEvent(
    Guid DeliveryId,
    Guid TenantId,
    Guid ApartmentId,
    DateTimeOffset DeliveredAtUtc,
    Guid? DeliveredToUserId);
