using Dominodo.E2E.Clients.Modules.Tenants.Models;

namespace Dominodo.E2E.Clients.Modules.Tenants;

/// <summary>
/// Result of <see cref="TenantsRequestBuilder.CreateApartmentAsync"/>: the resolved tenant slug (whether
/// supplied or freshly created) plus the persisted apartment. <see cref="Id"/> is a shortcut for
/// <c>Apartment.Id</c>.
/// </summary>
public sealed record CreatedApartment(string TenantSlug, ApartmentDetailModel Apartment)
{
    public Guid Id => Apartment.Id;
}

/// <summary>
/// Result of <see cref="TenantsRequestBuilder.CreateResidentAsync"/>: everything a test needs to act as (or
/// on) the resident — the resolved tenant slug, the apartment id, the resident user id (mint a token with
/// <c>JwtTokenFactory.CreateUserToken(UserId)</c>), and the created residency id.
/// </summary>
public sealed record CreatedResident(string TenantSlug, Guid ApartmentId, Guid UserId, Guid ResidentId);
