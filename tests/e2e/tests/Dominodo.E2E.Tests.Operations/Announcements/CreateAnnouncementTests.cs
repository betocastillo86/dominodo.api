using System.Net;
using Dominodo.E2E.Clients.Modules.Operations;
using Dominodo.E2E.Clients.Modules.Operations.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Operations.Announcements;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/announcements</c> (AnnouncementsController.Create), guarded by
/// <c>[HasPermission(Permissions.AnnouncementsCreate)]</c> and scoped by the <c>X-Tenant</c> header. Returns
/// 201 with { id } (a Draft). Authorization is proven on both branches (a Platform grant and a Tenant grant),
/// plus the tenant-mismatch paths (unknown slug ⇒ 400, wrong tenant ⇒ 403). The 400 cases cover every rule in
/// <c>CreateAnnouncementCommandValidator</c>.
/// </summary>
[TestFixture]
public sealed class CreateAnnouncementTests : BaseOperationsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel();

        // Act — no token
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksAnnouncementsCreate()
    {
        // Arrange — a bearer carrying announcements.view (not announcements.create): proves create needs its
        // own permission, distinct from the read permission that governs the admin listing.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds announcements.create only via its Active membership in the
        // seeded tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed: write isolation ⇒ 403.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsCreate);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break every FluentValidation rule evaluated once the body binds: Title NotEmpty,
        // Body NotEmpty, Category MaximumLength(100), and (with a valid ByTower type) the AudienceFilter-
        // required rule. AudienceType stays a valid enum name so binding succeeds and the validator runs.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel() with
        {
            Title = "",
            Body = "",
            Category = new string('x', 101),
            AudienceType = "ByTower",
            AudienceFilter = "",
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsCreate);

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug, token: token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewAnnouncementModel.Title))
                .ShouldHaveValidationError(nameof(NewAnnouncementModel.Body))
                .ShouldHaveValidationError(nameof(NewAnnouncementModel.Category))
                .ShouldHaveValidationError(nameof(NewAnnouncementModel.AudienceFilter));
    }

    [Test]
    public async Task _400_WhenTitleExceedsMaxLength()
    {
        // Arrange — Title's MaximumLength(200) rule; a non-empty value too long to coexist with the NotEmpty
        // case above.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel() with { Title = new string('x', 201) };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsCreate);

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewAnnouncementModel.Title));
    }

    [Test]
    public async Task _400_WhenAudienceTypeIsNotAValidEnum()
    {
        // Arrange — an unknown enum name fails at JSON binding (JsonStringEnumConverter); the
        // InvalidModelStateResponseFactory maps it to the same Validation.Failed shape as a validator error.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel() with { AudienceType = "NotAType" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsCreate);

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewAnnouncementModel.AudienceType));
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid create token, but an X-Tenant slug that does not resolve. TenantResolutionMiddleware
        // rejects before authorization or the handler run.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsCreate);

        // Act
        var response = await OperationsClient.CreateAnnouncement(
            model, tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _201_WhenUserHasAnnouncementsCreateOnPlatform_AndVerifiesByFetching()
    {
        // Arrange — Platform announcements.create holder (cross-tenant grant), creating in the seeded tenant.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel(
            audienceType: "ByTower", audienceFilter: OperationsRequestBuilder.TowerFilter("Torre A"),
            category: "Mantenimiento", priority: 1);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsCreate);

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug, token: token);

        // Assert — 201 with a non-empty Guid id.
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);

        // Verify round-trip: the announcement is fetchable and its data matches. New ones start as Draft.
        var announcement = await OperationsRequestBuilder.GetAnnouncementAsync(SeededTenantSlug, response.Content.Id);
        announcement.Title.ShouldBe(model.Title);
        announcement.Body.ShouldBe(model.Body);
        announcement.Category.ShouldBe(model.Category);
        announcement.Priority.ShouldBe(model.Priority);
        announcement.AudienceType.ShouldBe("ByTower");
        announcement.Status.ShouldBe("Draft");
        announcement.PublishedAtUtc.ShouldBeNull();
    }

    [Test]
    public async Task _201_WhenUserHasAnnouncementsCreateInTenant()
    {
        // Arrange — the seeded tenant user: announcements.create comes from its Active membership in the
        // seeded tenant (not a platform grant), so this exercises the tenant branch of resolution.
        var model = OperationsRequestBuilder.BuildNewAnnouncementModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsCreate);

        // Act
        var response = await OperationsClient.CreateAnnouncement(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }
}
