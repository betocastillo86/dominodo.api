namespace Dominodo.Operations.Contracts;

// List projection of a Request (domain-model §3.1). Enums are strings — Contracts stays thin and must
// not reference Domain.
public sealed record RequestDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Type,
    string? Category,
    string Title,
    string Status,
    string Priority,
    Guid CreatedByUserId,
    Guid? ApartmentId,
    Guid? AssignedToUserId);
