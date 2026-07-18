namespace Dominodo.Tenants.Contracts;

public sealed record TenantDto(
    Guid Id,
    string Slug,
    string Name,
    string Type,
    string Status,
    string City);
