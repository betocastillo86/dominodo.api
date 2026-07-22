using System.Net;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.NotificationTemplates;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/notification-templates/{id}</c>
/// (NotificationTemplatesController.Update), guarded by <c>[HasPermission(Permissions.NotificationsEdit)]</c>.
/// There is no create-by-API for templates (§4.2), so the target row is arranged via the dev-only SQL
/// endpoint as a GLOBAL default (TenantId NULL) and edited with a Platform-scope token (no X-Tenant), which
/// is the only scope whose scopeTenantId (null) matches the global row. Anonymous ⇒ 401; a bearer lacking
/// the permission ⇒ 403; enabling a channel without its content ⇒ 400 Validation.Failed; success is 204 and
/// the row is verified against <c>admin.NotificationTemplates</c> via dev SQL.
/// </summary>
[TestFixture]
public sealed class UpdateNotificationTemplateTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = AdminRequestBuilder.BuildUpdateNotificationTemplateModel();

        // Act — no token
        var response = await AdminClient.UpdateNotificationTemplate(Guid.NewGuid(), model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksNotificationsEdit()
    {
        // Arrange — bearer for the seeded "Rol Public" user: exists but carries zero permissions.
        var model = AdminRequestBuilder.BuildUpdateNotificationTemplateModel();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.UpdateNotificationTemplate(Guid.NewGuid(), model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenEnabledChannelsMissingContent()
    {
        // Arrange — break every UpdateNotificationTemplateCommandValidator rule at once: Id NotEmpty
        // (Guid.Empty), and each enabled channel requires its content (EmailEnabled ⇒ EmailSubject +
        // EmailBodyHtml, PushEnabled ⇒ PushText, InAppEnabled ⇒ InAppText).
        var model = AdminRequestBuilder.BuildUpdateNotificationTemplateModel() with
        {
            EmailEnabled = true,
            PushEnabled = true,
            InAppEnabled = true,
            EmailSubject = null,
            EmailBodyHtml = null,
            PushText = null,
            InAppText = null,
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.NotificationsEdit);

        // Act — Guid.Empty id also trips the Id NotEmpty rule.
        var response = await AdminClient.UpdateNotificationTemplate(Guid.Empty, model, token: token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateNotificationTemplateModel.EmailSubject))
                .ShouldHaveValidationError(nameof(UpdateNotificationTemplateModel.EmailBodyHtml))
                .ShouldHaveValidationError(nameof(UpdateNotificationTemplateModel.PushText))
                .ShouldHaveValidationError(nameof(UpdateNotificationTemplateModel.InAppText))
                .ShouldHaveValidationError("Id");
    }

    [Test]
    public async Task _204_UpdatesGlobalTemplate_AndPersistsContent()
    {
        // Arrange — a global default template, edited in Platform scope (scopeTenantId null matches TenantId null).
        var seeded = await AdminRequestBuilder.SeedGlobalNotificationTemplateAsync(NotificationTemplateTypes.RequestUpdated);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.NotificationsEdit);
        var model = AdminRequestBuilder.BuildUpdateNotificationTemplateModel(
            emailEnabled: true,
            pushEnabled: false,
            inAppEnabled: true,
            emailSubject: "Updated subject",
            emailBodyHtml: "<p>Updated body</p>",
            inAppText: "Updated in-app",
            isActive: false,
            localization: "es") with { PushText = null };

        // Act — no X-Tenant ⇒ global scope.
        var response = await AdminClient.UpdateNotificationTemplate(seeded.Id, model, token: token);

        // Assert — 204, and the persisted row carries the new content (verified via dev SQL).
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var row = await AdminRequestBuilder.FindNotificationTemplateByIdAsync(seeded.Id);
        row.ShouldNotBeNull();
        row!.TenantId.ShouldBeNull();
        row.EmailEnabled.ShouldBeTrue();
        row.PushEnabled.ShouldBeFalse();
        row.InAppEnabled.ShouldBeTrue();
        row.EmailSubject.ShouldBe("Updated subject");
        row.EmailBodyHtml.ShouldBe("<p>Updated body</p>");
        row.InAppText.ShouldBe("Updated in-app");
        row.PushText.ShouldBeNull();
        row.IsActive.ShouldBeFalse();
        row.Localization.ShouldBe("es");
    }
}
