using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Tenants;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/tenants</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsCreate)]</c> on the TenantsController. Returns 201 with { id }.
/// </summary>
[TestFixture]
public sealed class CreateTenantTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildNewTenantModel();

        // Act — no token
        var response = await TenantsClient.CreateTenant(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsCreate()
    {
        // Arrange — bearer carrying tenants.edit (not tenants.create): proves create needs its own
        // permission, distinct from the write permission that governs every other tenant mutation.
        var model = TenantsRequestBuilder.BuildNewTenantModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.CreateTenant(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break every CreateTenantCommandValidator rule at once:
        // Slug Matches("^[a-z0-9-]+$"), Name NotEmpty, Type enum-parse, and the profile MaximumLength rules.
        var model = TenantsRequestBuilder.BuildNewTenantModel() with
        {
            Slug = "Invalid Slug!",
            Name = "",
            Type = "NotAType",
            LegalId = new string('x', 51),
            Address = new string('x', 301),
            City = new string('x', 101),
            Country = new string('x', 101),
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsCreate);

        // Act
        var response = await TenantsClient.CreateTenant(model, token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewTenantModel.Slug))
                .ShouldHaveValidationError(nameof(NewTenantModel.Name))
                .ShouldHaveValidationError(nameof(NewTenantModel.Type))
                .ShouldHaveValidationError(nameof(NewTenantModel.LegalId))
                .ShouldHaveValidationError(nameof(NewTenantModel.Address))
                .ShouldHaveValidationError(nameof(NewTenantModel.City))
                .ShouldHaveValidationError(nameof(NewTenantModel.Country));
    }

    [Test]
    public async Task _400_WhenSlugExceedsMaxLength()
    {
        // Arrange — Slug's MaximumLength(100) rule; a value that satisfies the kebab regex but is too long,
        // so it can't coexist with the regex-invalid case above.
        var model = TenantsRequestBuilder.BuildNewTenantModel() with { Slug = new string('a', 101) };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsCreate);

        // Act
        var response = await TenantsClient.CreateTenant(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewTenantModel.Slug));
    }

    [Test]
    public async Task _201_CreatesTenant_AndVerifiesByFetching()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildNewTenantModel(
            type: "Mixto",
            legalId: "NIT-901555",
            branding: "{\"color\":\"#123456\"}",
            settings: "{\"tz\":\"America/Bogota\"}");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsCreate);

        // Act
        var response = await TenantsClient.CreateTenant(model, token);

        // Assert — 201 with a non-empty Guid id.
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = response.Content;
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);

        // Verify round-trip: the tenant is fetchable and its data matches what was sent. New tenants
        // start in Onboarding.
        var viewToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);
        var getResponse = await TenantsClient.GetTenantById(created.Id, viewToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tenant = getResponse.Content;
        tenant.ShouldNotBeNull();
        tenant!.Id.ShouldBe(created.Id);
        tenant.Slug.ShouldBe(model.Slug);
        tenant.Name.ShouldBe(model.Name);
        tenant.Type.ShouldBe(model.Type);
        tenant.LegalId.ShouldBe(model.LegalId);
        tenant.Address.ShouldBe(model.Address);
        tenant.City.ShouldBe(model.City);
        tenant.Country.ShouldBe(model.Country);
        tenant.Branding.ShouldBe(model.Branding);
        tenant.Settings.ShouldBe(model.Settings);
        tenant.Status.ShouldBe("Onboarding");
    }

    [Test]
    public async Task _409_WhenSlugAlreadyExists()
    {
        // Arrange — create a tenant, then attempt a second one reusing its slug.
        var existing = await TenantsRequestBuilder.CreateTenantAsync();
        var duplicate = TenantsRequestBuilder.BuildNewTenantModel(slug: existing.Slug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsCreate);

        // Act
        var response = await TenantsClient.CreateTenant(duplicate, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("Tenant.SlugAlreadyExists");
    }
}
