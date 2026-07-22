namespace Dominodo.Operations.Contracts;

public sealed record VisitDetailDto(
    Guid Id,
    Guid TenantId,
    Guid ApartmentId,
    string Type,
    string Status,
    string VisitorName,
    string? VisitorDocument,
    string? PhotoUrl,
    string? VehiclePlate,
    decimal? AmountPaid,
    Guid RegisteredByUserId,
    Guid? AuthorizedByUserId,
    DateTimeOffset EntryAtUtc,
    DateTimeOffset? ExitAtUtc,
    string? Metadata);
