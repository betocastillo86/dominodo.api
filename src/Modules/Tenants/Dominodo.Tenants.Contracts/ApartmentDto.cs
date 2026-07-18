namespace Dominodo.Tenants.Contracts;

public sealed record ApartmentDto(
    Guid Id,
    Guid TenantId,
    string? Tower,
    string Number,
    string Type,
    string Status);
