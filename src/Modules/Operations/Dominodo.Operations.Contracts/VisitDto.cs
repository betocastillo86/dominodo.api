namespace Dominodo.Operations.Contracts;

public sealed record VisitDto(
    Guid Id,
    Guid TenantId,
    Guid ApartmentId,
    string Type,
    string Status,
    string VisitorName,
    Guid RegisteredByUserId,
    DateTimeOffset EntryAtUtc,
    DateTimeOffset? ExitAtUtc);
