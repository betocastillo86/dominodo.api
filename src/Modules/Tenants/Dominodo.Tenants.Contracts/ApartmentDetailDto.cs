namespace Dominodo.Tenants.Contracts;

public sealed record ApartmentDetailDto(
    Guid Id,
    Guid TenantId,
    string? Tower,
    string Number,
    string Type,
    string Status,
    string? Attributes);
