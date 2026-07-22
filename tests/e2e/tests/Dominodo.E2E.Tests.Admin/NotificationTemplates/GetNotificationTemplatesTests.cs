using System.Net;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.NotificationTemplates;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/notification-templates</c> (NotificationTemplatesController.List),
/// guarded by <c>[HasPermission(Permissions.NotificationsView)]</c>. It lists the global default templates
/// (plus the current tenant's overrides when X-Tenant is sent). A global default is arranged via the
/// dev-only SQL endpoint and listed with a Platform-scope token (no X-Tenant). Anonymous ⇒ 401; a bearer
/// lacking the permission ⇒ 403; success is 200 with an array that includes the seeded template.
/// </summary>
[TestFixture]
public sealed class GetNotificationTemplatesTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await AdminClient.GetNotificationTemplates();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksNotificationsView()
    {
        // Arrange — bearer for the seeded "Rol Public" user: exists but carries zero permissions.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.GetNotificationTemplates(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_ReturnsTemplatesIncludingSeededGlobalDefault()
    {
        // Arrange — a global default template, listed in Platform scope.
        var seeded = await AdminRequestBuilder.SeedGlobalNotificationTemplateAsync(
            NotificationTemplateTypes.RequestClosed,
            emailEnabled: false,
            pushEnabled: false,
            inAppEnabled: true,
            emailSubject: null,
            emailBodyHtml: null,
            inAppText: "Seeded list in-app",
            pushText: null,
            isActive: true,
            localization: null);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.NotificationsView);

        // Act
        var response = await AdminClient.GetNotificationTemplates(token: token);

        // Assert — 200, and the seeded global default is present with its data.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var templates = response.Content;
        templates.ShouldNotBeNull();
        var mine = templates!.SingleOrDefault(t => t.Id == seeded.Id);
        mine.ShouldNotBeNull();
        mine!.TenantId.ShouldBeNull();
        mine.Type.ShouldBe("RequestClosed");
        mine.InAppEnabled.ShouldBeTrue();
        mine.InAppText.ShouldBe("Seeded list in-app");
    }
}
