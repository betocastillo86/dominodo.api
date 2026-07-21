using Dominodo.E2E.Clients.Common;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Clients.Modules.Users;
using Dominodo.E2E.Core;
using Dominodo.E2E.Core.Security;

namespace Dominodo.E2E.Clients.Modules.Tenants;

/// <summary>
/// Builds Tenants-module request models (fake but valid data by default) and composes full
/// <c>Arrange</c> use cases. Per README §8, any Arrange helper that calls the API throws on
/// non-success — a broken Arrange aborts the test rather than producing a misleading Assert.
/// Composite helpers (<see cref="CreateApartmentAsync"/>, <see cref="CreateResidentAsync"/>) take every
/// dependency as an optional parameter and create only what the caller did not supply (see the skill's
/// "self-completing Arrange" tip) — so a test that needs "some apartment somewhere" writes one line.
/// </summary>
public sealed class TenantsRequestBuilder(
    ITenantsClient tenants,
    UsersRequestBuilder users,
    JwtTokenFactory jwtTokenFactory)
    : BaseRequestBuilder
{
    private readonly ITenantsClient _tenants = tenants;
    private readonly UsersRequestBuilder _users = users;
    private readonly JwtTokenFactory _jwtTokenFactory = jwtTokenFactory;

    /// <summary>
    /// Builds a valid <see cref="NewTenantModel"/> (unique lowercase-kebab slug, default type "Conjunto").
    /// Any field is overridable: <c>model with { Type = "Edificio" }</c>. Does NOT call the API.
    /// </summary>
    public NewTenantModel BuildNewTenantModel(
        string? slug = null,
        string? name = null,
        string? type = null,
        string? address = null,
        string? city = null,
        string? country = null,
        string? legalId = null,
        string? branding = null,
        string? settings = null)
    {
        return new NewTenantModel
        {
            Slug = slug ?? $"e2e-{Guid.NewGuid():N}",
            Name = name ?? $"Conjunto {Faker.Address.City()}",
            Type = type ?? "Conjunto",
            Address = address ?? Faker.Address.StreetAddress(),
            City = city ?? Faker.Address.City(),
            Country = country ?? Faker.Address.Country(),
            LegalId = legalId,
            Branding = branding,
            Settings = settings,
        };
    }

    /// <summary>
    /// Full Arrange (parameter overload): builds a valid <see cref="NewTenantModel"/> from the given
    /// overrides and creates it. Convenience over <see cref="BuildNewTenantModel"/> +
    /// <see cref="CreateTenantAsync(NewTenantModel)"/> when you only need to tweak a field or two.
    /// </summary>
    public Task<TenantDetailModel> CreateTenantAsync(
        string? slug = null,
        string? name = null,
        string? type = null,
        string? address = null,
        string? city = null,
        string? country = null,
        string? legalId = null,
        string? branding = null,
        string? settings = null)
    {
        return CreateTenantAsync(
            BuildNewTenantModel(slug, name, type, address, city, country, legalId, branding, settings));
    }

    /// <summary>
    /// Full Arrange: creates the given tenant with the seeded <c>tenants.create</c> token, reads it back
    /// via <c>GET /tenants/{id}</c> (with the <c>tenants.view</c> token), and returns the persisted
    /// <see cref="TenantDetailModel"/>. Throws on any non-success step so a broken Arrange aborts the test.
    /// </summary>
    public async Task<TenantDetailModel> CreateTenantAsync(NewTenantModel model)
    {
        var createToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsCreate);

        var response = await _tenants.CreateTenant(model, createToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: creating a tenant returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return await GetTenantAsync(response.Content!.Id);
    }

    /// <summary>
    /// Reads a tenant back via <c>GET /tenants/{id}</c> (with the <c>tenants.view</c> token) and returns
    /// the persisted model — used to assert an endpoint's side effect. Throws on non-success.
    /// </summary>
    public async Task<TenantDetailModel> GetTenantAsync(Guid id)
    {
        var viewToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        var response = await _tenants.GetTenantById(id, viewToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: GET tenant {id} returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return response.Content!;
    }

    /// <summary>
    /// Builds a valid <see cref="UpdateTenantModel"/> with a fresh name/profile. Any field is overridable.
    /// Does NOT call the API.
    /// </summary>
    public UpdateTenantModel BuildUpdateTenantModel(
        string? name = null,
        string? legalId = null,
        string? address = null,
        string? city = null,
        string? country = null)
    {
        return new UpdateTenantModel
        {
            Name = name ?? $"Conjunto {Faker.Address.City()}",
            LegalId = legalId,
            Address = address ?? Faker.Address.StreetAddress(),
            City = city ?? Faker.Address.City(),
            Country = country ?? Faker.Address.Country(),
        };
    }

    /// <summary>Builds a valid <see cref="SetTenantFeatureModel"/>. Does NOT call the API.</summary>
    public SetTenantFeatureModel BuildSetTenantFeatureModel(bool enabled = true)
    {
        return new SetTenantFeatureModel { Enabled = enabled };
    }

    /// <summary>
    /// Full Arrange: enables/disables a feature for a tenant using the seeded <c>tenants.edit</c> token.
    /// Throws on non-success so a broken Arrange aborts the test immediately.
    /// </summary>
    public async Task SetTenantFeatureAsync(Guid tenantId, string featureKey, bool enabled = true)
    {
        var editToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        var response = await _tenants.SetTenantFeature(
            tenantId, featureKey, new SetTenantFeatureModel { Enabled = enabled }, editToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: setting feature '{featureKey}' on tenant {tenantId} returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }
    }

    /// <summary>
    /// Builds a valid <see cref="NewApartmentModel"/> (unique number, default type "Apartment"). Any field
    /// is overridable: <c>model with { Type = "House" }</c>. Does NOT call the API.
    /// </summary>
    public NewApartmentModel BuildNewApartmentModel(
        string? number = null,
        string? type = null,
        string? tower = null,
        string? attributes = null)
    {
        return new NewApartmentModel
        {
            Number = number ?? $"apt-{Guid.NewGuid():N}",
            Type = type ?? "Apartment",
            Tower = tower,
            Attributes = attributes,
        };
    }

    /// <summary>
    /// Builds a valid <see cref="UpdateApartmentModel"/> (unique number, default type "Apartment"). Any field
    /// is overridable: <c>model with { Type = "House" }</c>. Does NOT call the API.
    /// </summary>
    public UpdateApartmentModel BuildUpdateApartmentModel(
        string? number = null,
        string? type = null,
        string? tower = null,
        string? attributes = null)
    {
        return new UpdateApartmentModel
        {
            Number = number ?? $"apt-{Guid.NewGuid():N}",
            Type = type ?? "Apartment",
            Tower = tower,
            Attributes = attributes,
        };
    }

    /// <summary>
    /// Builds a valid <see cref="ChangeApartmentStatusModel"/> (default "Occupied", a valid transition from
    /// the Vacant state new apartments start in). Does NOT call the API.
    /// </summary>
    public ChangeApartmentStatusModel BuildChangeApartmentStatusModel(string status = "Occupied")
    {
        return new ChangeApartmentStatusModel { Status = status };
    }

    /// <summary>
    /// Self-completing Arrange: creates an apartment, creating its <b>tenant too</b> when
    /// <paramref name="tenantSlug"/> is not supplied. Every input is optional — pass a slug to place the
    /// apartment in an existing tenant, or nothing to get a brand-new tenant + apartment in one line. Uses a
    /// Platform <c>apartments.create</c> token (cross-tenant grant, works for any resolved tenant), reads the
    /// apartment back, and returns both the resolved slug and the persisted model. Throws on any non-success
    /// step so a broken Arrange aborts the test.
    /// </summary>
    public async Task<CreatedApartment> CreateApartmentAsync(
        string? tenantSlug = null,
        string? number = null,
        string? type = null,
        string? tower = null,
        string? attributes = null)
    {
        tenantSlug ??= (await CreateTenantAsync()).Slug;

        var model = BuildNewApartmentModel(number, type, tower, attributes);
        var createToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsCreate);

        var response = await _tenants.CreateApartment(model, tenantSlug, createToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: creating an apartment in tenant '{tenantSlug}' returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }

        var apartment = await GetApartmentAsync(tenantSlug, response.Content!.Id);
        return new CreatedApartment(tenantSlug, apartment);
    }

    /// <summary>
    /// Reads an apartment back via <c>GET /apartments/{id}</c> (Platform <c>apartments.view</c> token, scoped
    /// by <paramref name="tenantSlug"/>) and returns the persisted model. Throws on non-success.
    /// </summary>
    public async Task<ApartmentDetailModel> GetApartmentAsync(string tenantSlug, Guid id)
    {
        var viewToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        var response = await _tenants.GetApartmentById(id, tenantSlug, viewToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: GET apartment {id} in tenant '{tenantSlug}' returned " +
                $"{(int)response.StatusCode}. Body: {response.Error?.Content}");
        }

        return response.Content!;
    }

    /// <summary>
    /// Full Arrange: assigns <paramref name="userId"/> as an active resident of the apartment using a Platform
    /// <c>tenants.edit</c> token (cross-tenant, so it works for any resolved tenant), scoped by
    /// <paramref name="tenantSlug"/>. The user must already exist in Users. Returns the created resident id.
    /// Throws on non-success so a broken Arrange aborts the test.
    /// </summary>
    public async Task<Guid> AssignResidentAsync(
        string tenantSlug,
        Guid apartmentId,
        Guid userId,
        string relationType = "Owner",
        bool livesHere = true,
        DateOnly? startDate = null)
    {
        var editToken = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);
        var model = new AssignResidentModel
        {
            UserId = userId,
            RelationType = relationType,
            LivesHere = livesHere,
            StartDate = startDate,
        };

        var response = await _tenants.AssignResident(apartmentId, model, tenantSlug, editToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: assigning resident {userId} to apartment {apartmentId} in tenant " +
                $"'{tenantSlug}' returned {(int)response.StatusCode}. Body: {response.Error?.Content}");
        }

        return response.Content!.Id;
    }

    /// <summary>
    /// Self-completing Arrange: produces an apartment with an active resident, creating whatever the caller
    /// did not supply. Every input is optional and filled in only when missing:
    /// <list type="bullet">
    /// <item>no <paramref name="apartmentId"/> ⇒ creates an apartment (and a tenant, if no
    /// <paramref name="tenantSlug"/>);</item>
    /// <item>no <paramref name="userId"/> ⇒ registers a fresh user as the resident;</item>
    /// </list>
    /// So <c>CreateResidentAsync()</c> builds tenant + apartment + user + residency in one line, while
    /// <c>CreateResidentAsync(userId: mine)</c> reuses your user for everything else. Returns the resolved
    /// slug, apartment id, user id and residency id. Throws on any non-success step.
    /// </summary>
    public async Task<CreatedResident> CreateResidentAsync(
        string? tenantSlug = null,
        Guid? apartmentId = null,
        Guid? userId = null,
        string relationType = "Owner",
        bool livesHere = true,
        DateOnly? startDate = null,
        string? apartmentNumber = null,
        string? apartmentType = null,
        string? apartmentTower = null)
    {
        if (apartmentId is null)
        {
            var apartment = await CreateApartmentAsync(tenantSlug, apartmentNumber, apartmentType, apartmentTower);
            tenantSlug = apartment.TenantSlug;
            apartmentId = apartment.Id;
        }
        else if (tenantSlug is null)
        {
            throw new ArgumentException(
                "tenantSlug is required when apartmentId is supplied — an apartment is scoped to its tenant.",
                nameof(tenantSlug));
        }

        userId ??= (await _users.RegisterUserAsync()).Id;

        var residentId = await AssignResidentAsync(
            tenantSlug!, apartmentId.Value, userId.Value, relationType, livesHere, startDate);

        return new CreatedResident(tenantSlug!, apartmentId.Value, userId.Value, residentId);
    }
}
