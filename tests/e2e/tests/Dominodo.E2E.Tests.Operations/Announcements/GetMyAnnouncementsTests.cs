using System.Net;
using Dominodo.E2E.Clients.Modules.Operations;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Operations.Announcements;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/announcements/mine</c> (AnnouncementsController.Mine), a plain
/// <c>[Authorize]</c> (no permission) endpoint scoped by the <c>X-Tenant</c> header. It returns the ACTIVE
/// (published, unexpired) announcements narrowed to the caller's audience: AllTenant always, plus ByTower /
/// ByApartments matched against the caller's apartments/towers (resolved through the Tenants facade). Drafts
/// and non-matching audiences are excluded — that scoping is the guarantee under test.
/// </summary>
[TestFixture]
public sealed class GetMyAnnouncementsTests : BaseOperationsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await OperationsClient.GetMyAnnouncements(tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _200_ReturnsOnlyActiveAnnouncementsRelevantToTheResident()
    {
        // Arrange — one line builds a fresh tenant + apartment (in "TorreX") + user + active residency, so the
        // caller has a known audience (their apartment id and tower). All announcements below live in that same
        // tenant.
        const string myTower = "TorreX";
        var resident = await TenantsRequestBuilder.CreateResidentAsync(apartmentTower: myTower);
        var slug = resident.TenantSlug;

        // Should REACH the resident:
        var reachAll = await OperationsRequestBuilder.CreatePublishedAnnouncementAsync(
            tenantSlug: slug, audienceType: "AllTenant");
        var reachTower = await OperationsRequestBuilder.CreatePublishedAnnouncementAsync(
            tenantSlug: slug, audienceType: "ByTower",
            audienceFilter: OperationsRequestBuilder.TowerFilter(myTower));
        var reachApartment = await OperationsRequestBuilder.CreatePublishedAnnouncementAsync(
            tenantSlug: slug, audienceType: "ByApartments",
            audienceFilter: OperationsRequestBuilder.ApartmentsFilter(resident.ApartmentId));

        // Should NOT reach the resident:
        var otherTower = await OperationsRequestBuilder.CreatePublishedAnnouncementAsync(
            tenantSlug: slug, audienceType: "ByTower",
            audienceFilter: OperationsRequestBuilder.TowerFilter("OtraTorre"));
        var otherApartment = await OperationsRequestBuilder.CreatePublishedAnnouncementAsync(
            tenantSlug: slug, audienceType: "ByApartments",
            audienceFilter: OperationsRequestBuilder.ApartmentsFilter(Guid.NewGuid()));
        // An AllTenant announcement that is still a DRAFT (never published) — not active, so it must not appear.
        var unpublished = await OperationsRequestBuilder.CreateAnnouncementAsync(
            tenantSlug: slug, audienceType: "AllTenant");

        var token = JwtTokenFactory.CreateUserToken(resident.UserId);

        // Act
        var response = await OperationsClient.GetMyAnnouncements(pageSize: 100, tenant: slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();

        response.Content!.Items.ShouldContain(x => x.Id == reachAll.Id);
        response.Content!.Items.ShouldContain(x => x.Id == reachTower.Id);
        response.Content!.Items.ShouldContain(x => x.Id == reachApartment.Id);

        response.Content!.Items.ShouldNotContain(x => x.Id == otherTower.Id);
        response.Content!.Items.ShouldNotContain(x => x.Id == otherApartment.Id);
        response.Content!.Items.ShouldNotContain(x => x.Id == unpublished.Id);
    }
}
