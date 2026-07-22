namespace Dominodo.Operations.Contracts;

public sealed record DeliveryDto(
    Guid Id,
    Guid TenantId,
    string Code,
    Guid ApartmentId,
    string Type,
    string Status,
    Guid RegisteredByUserId,
    string? Carrier,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? DeliveredAtUtc);
