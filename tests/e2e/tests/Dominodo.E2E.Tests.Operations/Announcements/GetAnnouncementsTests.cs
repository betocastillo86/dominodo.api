using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Operations.Announcements;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/announcements</c> (AnnouncementsController.List), guarded by
/// <c>[HasPermission(Permissions.AnnouncementsView)]</c> and scoped by the <c>X-Tenant</c> header. The admin
/// listing includes drafts. Authorization is proven on both branches (Platform + Tenant grant) plus tenant
/// isolation. There is no request validator (pagination clamped in <c>PageRequest</c>), so the only 400 is
/// <c>Tenant.Unknown</c> from the resolution middleware.
/// </summary>
[TestFixture]
public sealed class GetAnnouncementsTests : BaseOperationsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await OperationsClient.GetAnnouncements(tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksAnnouncementsView()
    {
        // Arrange — a real, existing user assigned to a Platform role that carries ZERO permissions, against a
        // valid tenant so the 403 is unambiguously "missing announcements.view" (not a bad/missing tenant).
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await OperationsClient.GetAnnouncements(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds announcements.view only via an Active membership in the seeded
        // tenant. Targeting a *different* (freshly created) tenant resolves to only its platform permissions
        // (none), so authorization fails closed: tenant isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsView);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await OperationsClient.GetAnnouncements(tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid announcements.view token, but an X-Tenant slug that does not resolve.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncements(tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _200_WhenUserHasAnnouncementsViewOnPlatform()
    {
        // Arrange — the seeded Platform user holds announcements.view cross-tenant.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncements(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
    }

    [Test]
    public async Task _200_WhenUserHasAnnouncementsViewInTenant()
    {
        // Arrange — the seeded tenant user: announcements.view via its Active membership (tenant branch).
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncements(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
    }

    [Test]
    public async Task _200_ReturnsAnnouncementsScopedToTenant_IncludingDrafts()
    {
        // Arrange — a draft in tenant A (the builder creates the tenant) and another in tenant B. The listing
        // must be scoped to the resolved tenant (A never leaks B) and must include drafts.
        var a = await OperationsRequestBuilder.CreateAnnouncementAsync();
        var b = await OperationsRequestBuilder.CreateAnnouncementAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncements(pageSize: 100, tenant: a.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Items.ShouldContain(x => x.Id == a.Id && x.Status == "Draft");
        response.Content!.Items.ShouldNotContain(x => x.Id == b.Id);
    }

    [Test]
    public async Task _200_FiltersByStatus()
    {
        // Arrange — in one tenant: a draft and a published announcement. Filtering status=Draft returns only
        // the draft.
        var draft = await OperationsRequestBuilder.CreateAnnouncementAsync();
        var published = await OperationsRequestBuilder.CreatePublishedAnnouncementAsync(tenantSlug: draft.TenantSlug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.AnnouncementsView);

        // Act
        var response = await OperationsClient.GetAnnouncements(
            status: "Draft", pageSize: 100, tenant: draft.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Items.ShouldContain(x => x.Id == draft.Id);
        response.Content!.Items.ShouldNotContain(x => x.Id == published.Id);
    }
}
