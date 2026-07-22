using System.Net;
using Dominodo.E2E.Clients.Modules.Operations.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Operations.Announcements;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/announcements/{id}</c> (AnnouncementsController.Update), guarded by
/// <c>[HasPermission(Permissions.AnnouncementsEdit)]</c> and scoped by the <c>X-Tenant</c> header. Success is
/// 204 NoContent. Authorization is proven on both branches (Platform + Tenant grant) plus the tenant-mismatch
/// path (403); an unknown id ⇒ 404. The 400 cases cover every rule in <c>UpdateAnnouncementCommandValidator</c>.
/// </summary>
[TestFixture]
public sealed class UpdateAnnouncementTests : BaseOperationsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel();

        // Act — no token
        var response = await OperationsClient.UpdateAnnouncement(Guid.NewGuid(), model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksAnnouncementsEdit()
    {
        // Arrange — a bearer carrying announcements.view (not announcements.edit): proves edit needs its own
        // permission, distinct from the read permission.
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.UpdateAnnouncement(
            Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds announcements.edit only via its Active membership in the
        // seeded tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed before the handler runs: write isolation ⇒ 403.
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsEdit);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await OperationsClient.UpdateAnnouncement(
            Guid.NewGuid(), model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — an existing draft to edit, then a body breaking every rule: Title NotEmpty, Body NotEmpty,
        // Category MaximumLength(100), and (with a valid ByTower type) the AudienceFilter-required rule.
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel() with
        {
            Title = "",
            Body = "",
            Category = new string('x', 101),
            AudienceType = "ByTower",
            AudienceFilter = "",
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.UpdateAnnouncement(draft.Id, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateAnnouncementModel.Title))
                .ShouldHaveValidationError(nameof(UpdateAnnouncementModel.Body))
                .ShouldHaveValidationError(nameof(UpdateAnnouncementModel.Category))
                .ShouldHaveValidationError(nameof(UpdateAnnouncementModel.AudienceFilter));
    }

    [Test]
    public async Task _400_WhenAudienceTypeIsNotAValidEnum()
    {
        // Arrange — an unknown enum name fails at JSON binding, mapped to the Validation.Failed shape.
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel() with { AudienceType = "NotAType" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.UpdateAnnouncement(draft.Id, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateAnnouncementModel.AudienceType));
    }

    [Test]
    public async Task _404_WhenAnnouncementDoesNotExist()
    {
        // Arrange — a valid edit token and body, but an id that does not exist in the resolved tenant. The
        // request passes auth + validation and reaches the handler, which returns Announcement.NotFound.
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.UpdateAnnouncement(
            Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Announcement.NotFound");
    }

    [Test]
    public async Task _204_WhenUserHasAnnouncementsEditOnPlatform_AndPersistsTheEdit()
    {
        // Arrange — a draft, edited by a Platform announcements.edit holder (cross-tenant grant).
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel(
            title: "Título editado", body: "Cuerpo editado", priority: 2, category: "Seguridad");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.UpdateAnnouncement(draft.Id, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the edit persisted.
        var updated = await OperationsRequestBuilder.GetAnnouncementAsync(SeededTenantSlug, draft.Id);
        updated.Title.ShouldBe(model.Title);
        updated.Body.ShouldBe(model.Body);
        updated.Priority.ShouldBe(model.Priority);
        updated.Category.ShouldBe(model.Category);
    }

    [Test]
    public async Task _204_WhenUserHasAnnouncementsEditInTenant()
    {
        // Arrange — a draft in the seeded tenant, edited by the seeded tenant user (grant via Active
        // membership), exercising the tenant branch of resolution.
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var model = OperationsRequestBuilder.BuildUpdateAnnouncementModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.UpdateAnnouncement(draft.Id, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
