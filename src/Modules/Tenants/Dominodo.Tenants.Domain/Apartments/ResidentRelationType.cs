namespace Dominodo.Tenants.Domain.Apartments;

// The physical link type between a person and an apartment (domain-model §2.4). This is NOT an RBAC role.
// 'Renter' (not 'Tenant') avoids colliding with the conjunto/Tenant aggregate.
public enum ResidentRelationType
{
    Owner,
    Renter
}
