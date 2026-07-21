using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/apartments</c> (ApartmentsController.Create), guarded by
/// <c>[HasPermission(Permissions.ApartmentsCreate)]</c> and scoped by the <c>X-Tenant</c> header. Returns
/// 201 with { id }; the (Tower, Number) uniqueness check is per-tenant (409). Authorization is proven on both
/// branches (a Platform grant and a Tenant grant), plus the tenant-mismatch paths (unknown slug ⇒ 400, wrong
/// tenant ⇒ 403). The 400 cases cover every rule in <c>CreateApartmentCommandValidator</c>.
/// </summary>
[TestFixture]
public sealed class CreateApartmentTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildNewApartmentModel();

        // Act — no token
        var response = await TenantsClient.CreateApartment(model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsCreate()
    {
        // Arrange — a bearer carrying apartments.edit (not apartments.create): proves create needs its own
        // permission, distinct from the write permission that governs updates.
        var model = TenantsRequestBuilder.BuildNewApartmentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.CreateApartment(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds apartments.create only via its Active membership in the
        // seeded tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed: write isolation ⇒ 403.
        var model = TenantsRequestBuilder.BuildNewApartmentModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsCreate);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await TenantsClient.CreateApartment(model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid create token, but an X-Tenant slug that does not resolve. TenantResolutionMiddleware
        // rejects before authorization or the handler run.
        var model = TenantsRequestBuilder.BuildNewApartmentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsCreate);

        // Act
        var response = await TenantsClient.CreateApartment(
            model, tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break every CreateApartmentCommandValidator rule at once: Number NotEmpty, Type enum-parse,
        // Tower MaximumLength(50).
        var model = TenantsRequestBuilder.BuildNewApartmentModel() with
        {
            Number = "",
            Type = "NotAType",
            Tower = new string('x', 51),
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsCreate);

        // Act
        var response = await TenantsClient.CreateApartment(model, tenant: SeededTenantSlug, token: token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewApartmentModel.Number))
                .ShouldHaveValidationError(nameof(NewApartmentModel.Type))
                .ShouldHaveValidationError(nameof(NewApartmentModel.Tower));
    }

    [Test]
    public async Task _400_WhenNumberExceedsMaxLength()
    {
        // Arrange — Number's MaximumLength(50) rule; a non-empty value too long to coexist with the NotEmpty
        // case above.
        var model = TenantsRequestBuilder.BuildNewApartmentModel() with { Number = new string('x', 51) };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsCreate);

        // Act
        var response = await TenantsClient.CreateApartment(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewApartmentModel.Number));
    }

    [Test]
    public async Task _201_WhenUserHasApartmentsCreateOnPlatform_AndVerifiesByFetching()
    {
        // Arrange — Platform apartments.create holder (cross-tenant grant), creating in the seeded tenant.
        var model = TenantsRequestBuilder.BuildNewApartmentModel(
            type: "House", tower: "Torre A", attributes: "{\"area\":80}");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsCreate);

        // Act
        var response = await TenantsClient.CreateApartment(model, tenant: SeededTenantSlug, token: token);

        // Assert — 201 with a non-empty Guid id.
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);

        // Verify round-trip: the apartment is fetchable and its data matches. New apartments start Vacant.
        var apartment = await TenantsRequestBuilder.GetApartmentAsync(SeededTenantSlug, response.Content.Id);
        apartment.Number.ShouldBe(model.Number);
        apartment.Type.ShouldBe(model.Type);
        apartment.Tower.ShouldBe(model.Tower);
        apartment.Attributes.ShouldBe(model.Attributes);
        apartment.Status.ShouldBe("Vacant");
    }

    [Test]
    public async Task _201_WhenUserHasApartmentsCreateInTenant()
    {
        // Arrange — the seeded tenant user: apartments.create comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of resolution.
        var model = TenantsRequestBuilder.BuildNewApartmentModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsCreate);

        // Act
        var response = await TenantsClient.CreateApartment(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task _409_WhenTowerAndNumberAlreadyExistInTenant()
    {
        // Arrange — create an apartment, then attempt a second one reusing its (Tower, Number) in the SAME
        // tenant. The uniqueness check is tenant-scoped.
        var existing = await TenantsRequestBuilder.CreateApartmentAsync(tower: "Torre Z");
        var duplicate = TenantsRequestBuilder.BuildNewApartmentModel(
            number: existing.Apartment.Number, tower: existing.Apartment.Tower);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsCreate);

        // Act
        var response = await TenantsClient.CreateApartment(duplicate, tenant: existing.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("Apartment.AlreadyExists");
    }
}
