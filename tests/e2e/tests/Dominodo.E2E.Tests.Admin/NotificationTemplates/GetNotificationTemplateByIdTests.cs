using System.Net;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.NotificationTemplates;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/notification-templates/{id}</c>
/// (NotificationTemplatesController.GetById), guarded by <c>[HasPermission(Permissions.NotificationsView)]</c>.
/// The target row is arranged via the dev-only SQL endpoint as a GLOBAL default (TenantId NULL), which is
/// readable in any scope, and read with a Platform-scope token (no X-Tenant). Anonymous ⇒ 401; a bearer
/// lacking the permission ⇒ 403; success is 200 with the template.
/// </summary>
[TestFixture]
public sealed class GetNotificationTemplateByIdTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await AdminClient.GetNotificationTemplateById(Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksNotificationsView()
    {
        // Arrange — bearer for the seeded "Rol Public" user: exists but carries zero permissions.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.GetNotificationTemplateById(Guid.NewGuid(), token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_ReturnsGlobalTemplate()
    {
        // Arrange — a global default template, read in Platform scope.
        var seeded = await AdminRequestBuilder.SeedGlobalNotificationTemplateAsync(
            NotificationTemplateTypes.RequestOpened,
            emailEnabled: true,
            pushEnabled: false,
            inAppEnabled: true,
            emailSubject: "Seeded subject",
            emailBodyHtml: "<p>Seeded body</p>",
            inAppText: "Seeded in-app",
            pushText: null,
            isActive: true,
            localization: "es");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.NotificationsView);

        // Act
        var response = await AdminClient.GetNotificationTemplateById(seeded.Id, token: token);

        // Assert — 200 with the seeded template's data.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var template = response.Content;
        template.ShouldNotBeNull();
        template!.Id.ShouldBe(seeded.Id);
        template.TenantId.ShouldBeNull();
        template.Type.ShouldBe("RequestOpened");
        template.EmailEnabled.ShouldBeTrue();
        template.PushEnabled.ShouldBeFalse();
        template.InAppEnabled.ShouldBeTrue();
        template.EmailSubject.ShouldBe("Seeded subject");
        template.EmailBodyHtml.ShouldBe("<p>Seeded body</p>");
        template.InAppText.ShouldBe("Seeded in-app");
        template.PushText.ShouldBeNull();
        template.IsActive.ShouldBeTrue();
        template.Localization.ShouldBe("es");
    }
}
