namespace Dominodo.Tenants.Domain.Tenants;

// A feature flag for a conjunto (domain-model §2.2) — a child entity under the Tenant aggregate, mutated
// only through Tenant. Explicit rows (not JSON) because features gate behaviour and are queried
// ("which tenants have paquetería?").
public sealed class TenantFeature
{
    private TenantFeature() { } // EF Core

    internal TenantFeature(Guid id, Guid tenantId, FeatureKey featureKey, bool enabled)
    {
        Id = id;
        TenantId = tenantId;
        FeatureKey = featureKey;
        Enabled = enabled;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public FeatureKey FeatureKey { get; private set; }
    public bool Enabled { get; private set; }

    internal void SetEnabled(bool enabled) => Enabled = enabled;
}
