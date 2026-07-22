namespace Dominodo.Operations.Contracts;

// Minimal cross-module read projection of a Request (domain-model §3.5) — e.g. super-admin dashboards.
public sealed record RequestSummaryDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Type,
    string Status,
    string Priority,
    string Title,
    Guid CreatedByUserId,
    Guid? AssignedToUserId);
