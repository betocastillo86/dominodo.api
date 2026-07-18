namespace Dominodo.Tenants.Contracts;

public sealed record TenantFeatureDto(
    Guid Id,
    Guid TenantId,
    string FeatureKey,
    bool Enabled);
