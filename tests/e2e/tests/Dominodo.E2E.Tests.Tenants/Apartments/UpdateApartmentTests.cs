using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/apartments/{id}</c> (ApartmentsController.Update), guarded by
/// <c>[HasPermission(Permissions.ApartmentsEdit)]</c> and scoped by the <c>X-Tenant</c> header. Success is
/// 204 NoContent. Authorization is proven on both branches (a Platform grant and a Tenant grant), plus the
/// tenant-mismatch paths (unknown slug ⇒ 400, wrong tenant ⇒ 403), an unknown id ⇒ 404, and a colliding
/// (Tower, Number) ⇒ 409. The 400 cases cover every rule in <c>UpdateApartmentCommandValidator</c>.
/// </summary>
[TestFixture]
public sealed class UpdateApartmentTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel();

        // Act — no token
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsEdit()
    {
        // Arrange — a bearer carrying apartments.view (not apartments.edit): proves the mutation needs the
        // edit permission, distinct from the read permission.
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds apartments.edit only via its Active membership in the seeded
        // tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed: write isolation ⇒ 403.
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenResidentTriesToEditTheirApartment()
    {
        // Arrange — a resident of the apartment. Residency grants READ access (GetById is dual-mode) but NOT
        // edit: Update requires apartments.edit, which the resident does not hold. Proves residency is not a
        // write grant.
        var resident = await TenantsRequestBuilder.CreateResidentAsync();
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel();
        var token = JwtTokenFactory.CreateUserToken(resident.UserId);

        // Act
        var response = await TenantsClient.UpdateApartment(
            resident.ApartmentId, model, tenant: resident.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid edit token, but an X-Tenant slug that does not resolve. TenantResolutionMiddleware
        // rejects before authorization or the handler run.
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(
            Guid.NewGuid(), model, tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break the FluentValidation rules evaluated once the body binds: Number NotEmpty,
        // Tower MaximumLength(50). (Type is an enum rejected at JSON binding — covered separately below.)
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel() with
        {
            Number = "",
            Tower = new string('x', 51),
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateApartmentModel.Number))
                .ShouldHaveValidationError(nameof(UpdateApartmentModel.Tower));
    }

    [Test]
    public async Task _400_WhenTypeIsNotAValidEnum()
    {
        // Arrange — an unknown enum name fails at JSON binding (JsonStringEnumConverter); the
        // InvalidModelStateResponseFactory maps it to the same Validation.Failed shape as a validator error.
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel() with { Type = "NotAType" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateApartmentModel.Type));
    }

    [Test]
    public async Task _400_WhenNumberExceedsMaxLength()
    {
        // Arrange — Number's MaximumLength(50) rule; a non-empty value too long to coexist with the NotEmpty
        // case above.
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel() with { Number = new string('x', 51) };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateApartmentModel.Number));
    }

    [Test]
    public async Task _404_WhenApartmentDoesNotExist()
    {
        // Arrange — a valid edit token and a resolvable tenant, but an id that exists in no apartment (scoped
        // to the resolved tenant).
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Apartment.NotFound");
    }

    [Test]
    public async Task _204_WhenUserHasApartmentsEditOnPlatform_AndVerifiesByFetching()
    {
        // Arrange — Platform apartments.edit holder (cross-tenant grant) updating an existing apartment.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync(type: "Apartment", tower: "Torre A");
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel(
            type: "House", tower: "Torre B", attributes: "{\"area\":120}");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(apartment.Id, model, tenant: apartment.TenantSlug, token: token);

        // Assert — 204, and the changes are persisted.
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var updated = await TenantsRequestBuilder.GetApartmentAsync(apartment.TenantSlug, apartment.Id);
        updated.Number.ShouldBe(model.Number);
        updated.Type.ShouldBe(model.Type);
        updated.Tower.ShouldBe(model.Tower);
        updated.Attributes.ShouldBe(model.Attributes);
    }

    [Test]
    public async Task _204_WhenUserHasApartmentsEditInTenant()
    {
        // Arrange — the seeded tenant user: apartments.edit comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of resolution.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync(tenantSlug: SeededTenantSlug);
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel(type: "Commercial");
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(apartment.Id, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task _409_WhenTowerAndNumberCollideWithAnotherApartment()
    {
        // Arrange — two apartments sharing a tower in the SAME tenant; then rename the first onto the second's
        // (Tower, Number). The uniqueness check is tenant-scoped.
        var first = await TenantsRequestBuilder.CreateApartmentAsync(tower: "Torre Q");
        var second = await TenantsRequestBuilder.CreateApartmentAsync(tenantSlug: first.TenantSlug, tower: "Torre Q");
        var model = TenantsRequestBuilder.BuildUpdateApartmentModel(
            number: second.Apartment.Number, tower: second.Apartment.Tower);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.UpdateApartment(first.Id, model, tenant: first.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("Apartment.AlreadyExists");
    }
}
