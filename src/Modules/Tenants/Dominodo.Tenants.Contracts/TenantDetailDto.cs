namespace Dominodo.Tenants.Contracts;

public sealed record TenantDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? LegalId,
    string Type,
    string Status,
    string Address,
    string City,
    string Country,
    string? Branding,
    string? Settings);
