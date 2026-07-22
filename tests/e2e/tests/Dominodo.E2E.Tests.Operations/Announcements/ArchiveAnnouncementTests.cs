using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Operations.Announcements;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/announcements/{id}/archive</c> (AnnouncementsController.Archive),
/// guarded by <c>[HasPermission(Permissions.AnnouncementsEdit)]</c> and scoped by the <c>X-Tenant</c> header.
/// Draft/Published → Archived. Success is 204 NoContent. No request body/validator, so there is no 400; an
/// unknown id ⇒ 404. Authorization is proven on both branches (Platform + Tenant grant) plus tenant isolation.
/// </summary>
[TestFixture]
public sealed class ArchiveAnnouncementTests : BaseOperationsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await OperationsClient.ArchiveAnnouncement(Guid.NewGuid(), tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksAnnouncementsEdit()
    {
        // Arrange — a bearer carrying announcements.view (not announcements.edit): archive is an edit operation.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.ArchiveAnnouncement(
            Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user's announcements.edit resolves only in the seeded tenant. Targeting a
        // different tenant fails authorization before the handler runs: write isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsEdit);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await OperationsClient.ArchiveAnnouncement(
            Guid.NewGuid(), tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _404_WhenAnnouncementDoesNotExist()
    {
        // Arrange — a valid edit token, but an id that does not exist in the resolved tenant.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.ArchiveAnnouncement(
            Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Announcement.NotFound");
    }

    [Test]
    public async Task _204_WhenUserHasAnnouncementsEditOnPlatform_AndTransitionsToArchived()
    {
        // Arrange — a fresh draft, archived by a Platform announcements.edit holder (cross-tenant grant).
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.ArchiveAnnouncement(draft.Id, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the transition.
        var archived = await OperationsRequestBuilder.GetAnnouncementAsync(SeededTenantSlug, draft.Id);
        archived.Status.ShouldBe("Archived");
    }

    [Test]
    public async Task _204_WhenUserHasAnnouncementsEditInTenant()
    {
        // Arrange — a draft in the seeded tenant, archived by the seeded tenant user (tenant branch).
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsEdit);

        // Act
        var response = await OperationsClient.ArchiveAnnouncement(draft.Id, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
