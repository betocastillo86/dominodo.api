using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Operations.Announcements;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/announcements/{id}</c> (AnnouncementsController.GetById), guarded by
/// <c>[HasPermission(Permissions.AnnouncementsView)]</c> and scoped by the <c>X-Tenant</c> header. Returns the
/// detail DTO (incl. drafts). Authorization is proven on both branches (Platform + Tenant grant) plus tenant
/// isolation; an unknown id (or one in another tenant) ⇒ 404 Announcement.NotFound.
/// </summary>
[TestFixture]
public sealed class GetAnnouncementByIdTests : BaseOperationsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token (auth is checked before anything, so no announcement need exist)
        var response = await OperationsClient.GetAnnouncementById(Guid.NewGuid(), tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksAnnouncementsView()
    {
        // Arrange — a real user with zero permissions, against a valid tenant so the 403 is unambiguously the
        // missing permission (GetById is permission-gated, not leak-safe/dual-mode like apartments).
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await OperationsClient.GetAnnouncementById(
            Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user's announcements.view resolves only in the seeded tenant. Targeting a
        // different tenant fails authorization before the handler runs: tenant isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsView);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await OperationsClient.GetAnnouncementById(
            Guid.NewGuid(), tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _404_WhenAnnouncementDoesNotExist()
    {
        // Arrange — a valid view token, but an id that does not exist in the resolved tenant.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncementById(
            Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Announcement.NotFound");
    }

    [Test]
    public async Task _200_WhenUserHasAnnouncementsViewOnPlatform()
    {
        // Arrange — an announcement (+ its tenant), read by a Platform announcements.view holder.
        var announcement = await OperationsRequestBuilder.CreateAnnouncementAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncementById(
            announcement.Id, tenant: announcement.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldBe(announcement.Id);
    }

    [Test]
    public async Task _200_WhenUserHasAnnouncementsViewInTenant()
    {
        // Arrange — the announcement must live in the seeded tenant, because the seeded tenant user's
        // announcements.view grant resolves only there. This exercises the tenant branch.
        var announcement = await OperationsRequestBuilder.CreateAnnouncementAsync(tenantSlug: SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncementById(
            announcement.Id, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldBe(announcement.Id);
    }
}
