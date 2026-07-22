namespace Dominodo.Operations.Contracts;

public sealed record DeliveryDetailDto(
    Guid Id,
    Guid TenantId,
    string Code,
    Guid ApartmentId,
    string Type,
    string Status,
    Guid RegisteredByUserId,
    string? Carrier,
    string? Comment,
    string? PhotoUrl,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    string? ReceivedByName,
    Guid? DeliveredToUserId,
    string? Metadata);
