using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments.Residents;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/apartments/{apartmentId}/residents</c>
/// (ApartmentResidentsController.Assign), guarded by <c>[HasPermission(Permissions.ApartmentsEdit)]</c> and
/// scoped by the <c>X-Tenant</c> header. Returns 201 with { id }. Authorization is proven on both branches (a
/// Platform grant and a Tenant grant), that residency is not a write grant, plus the tenant-mismatch paths
/// (unknown slug ⇒ 400, wrong tenant ⇒ 403). The 400 case covers every rule in
/// <c>AssignResidentCommandValidator</c>.
/// </summary>
[TestFixture]
public sealed class AssignResidentTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildAssignResidentModel();

        // Act — no token
        var response = await TenantsClient.AssignResident(Guid.NewGuid(), model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsEdit()
    {
        // Arrange — a bearer carrying apartments.view (not apartments.edit): proves assigning a resident needs
        // the edit permission, distinct from the read permission.
        var model = TenantsRequestBuilder.BuildAssignResidentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.AssignResident(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenResidentTriesToAddResidentToTheirApartment()
    {
        // Arrange — a resident of the apartment. Residency grants READ access (GetById is dual-mode) but NOT
        // write: assigning a resident requires apartments.edit, which the resident does not hold. Proves a
        // resident cannot add residents to their own apartment.
        var resident = await TenantsRequestBuilder.CreateResidentAsync();
        var model = TenantsRequestBuilder.BuildAssignResidentModel();
        var token = JwtTokenFactory.CreateUserToken(resident.UserId);

        // Act — the resident tries to assign another resident to their own apartment.
        var response = await TenantsClient.AssignResident(
            resident.ApartmentId, model, tenant: resident.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds apartments.edit only via its Active membership in the seeded
        // tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed: write isolation ⇒ 403.
        var model = TenantsRequestBuilder.BuildAssignResidentModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await TenantsClient.AssignResident(Guid.NewGuid(), model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid edit token, but an X-Tenant slug that does not resolve. TenantResolutionMiddleware
        // rejects before authorization or the handler run.
        var model = TenantsRequestBuilder.BuildAssignResidentModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.AssignResident(
            Guid.NewGuid(), model, tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break the FluentValidation rule evaluated once the body binds: UserId NotEmpty.
        // (RelationType is an enum rejected at JSON binding — covered separately below.)
        var model = TenantsRequestBuilder.BuildAssignResidentModel() with
        {
            UserId = Guid.Empty,
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.AssignResident(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(AssignResidentModel.UserId));
    }

    [Test]
    public async Task _400_WhenRelationTypeIsNotAValidEnum()
    {
        // Arrange — an unknown enum name fails at JSON binding (JsonStringEnumConverter); the
        // InvalidModelStateResponseFactory maps it to the same Validation.Failed shape as a validator error.
        var model = TenantsRequestBuilder.BuildAssignResidentModel() with { RelationType = "NotARelation" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.AssignResident(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(AssignResidentModel.RelationType));
    }

    [Test]
    public async Task _201_WhenUserHasApartmentsEditOnPlatform()
    {
        // Arrange — Platform apartments.edit holder (cross-tenant grant) assigning a fresh user to an apartment
        // in a brand-new tenant.
        var arrange = await TenantsRequestBuilder.CreateApartmentWithCandidateAsync();
        var model = TenantsRequestBuilder.BuildAssignResidentModel(userId: arrange.UserId, relationType: "Renter");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.AssignResident(
            arrange.ApartmentId, model, tenant: arrange.TenantSlug, token: token);

        // Assert — 201 with a non-empty resident id.
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task _201_WhenUserHasApartmentsEditInTenant()
    {
        // Arrange — the seeded tenant user: apartments.edit comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of resolution. The apartment and
        // candidate user must live in that same seeded tenant.
        var arrange = await TenantsRequestBuilder.CreateApartmentWithCandidateAsync(tenantSlug: SeededTenantSlug);
        var model = TenantsRequestBuilder.BuildAssignResidentModel(userId: arrange.UserId);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.AssignResident(
            arrange.ApartmentId, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }
}
