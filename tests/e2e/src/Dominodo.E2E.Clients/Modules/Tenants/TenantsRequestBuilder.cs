using Dominodo.E2E.Clients.Common;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Core.Security;

namespace Dominodo.E2E.Clients.Modules.Tenants;

/// <summary>
/// Builds Tenants-module request models (fake but valid data by default) and composes full
/// <c>Arrange</c> use cases. Per README §8, any Arrange helper that calls the API throws on
/// non-success — a broken Arrange aborts the test rather than producing a misleading Assert.
/// </summary>
public sealed class TenantsRequestBuilder(ITenantsClient tenants, JwtTokenFactory jwtTokenFactory)
    : BaseRequestBuilder
{
    private readonly ITenantsClient _tenants = tenants;
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
}
