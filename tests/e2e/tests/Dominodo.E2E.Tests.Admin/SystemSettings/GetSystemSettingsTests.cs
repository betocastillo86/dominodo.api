using System.Net;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.SystemSettings;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/system-settings</c> (SystemSettingsController.List), guarded by
/// <c>[HasPermission(Permissions.SettingsView)]</c>. Lists the global settings (plus the current tenant's
/// overrides when X-Tenant is sent) as a paged envelope (<c>PagedResult&lt;SystemSettingDto&gt;</c>).
/// Anonymous ⇒ 401; a bearer lacking the permission ⇒ 403; success is 200 with the paged envelope that
/// includes a just-created global row.
/// </summary>
[TestFixture]
public sealed class GetSystemSettingsTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await AdminClient.GetSystemSettings();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksSettingsView()
    {
        // Arrange — a real, existing user with ZERO permissions.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.GetSystemSettings(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_ReturnsPagedSettingsIncludingCreated()
    {
        // Arrange — a global setting to find in the list, and a large page size so it lands on page 1.
        var created = await AdminRequestBuilder.CreateSystemSettingAsync(value: "listed-value", valueType: "String");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsView);

        // Act
        var response = await AdminClient.GetSystemSettings(page: 1, pageSize: 100, token: token);

        // Assert — 200 and a paged envelope that includes the created global row.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = response.Content;
        page.ShouldNotBeNull();
        page!.Items.ShouldNotBeEmpty();
        var mine = page.Items.SingleOrDefault(s => s.Key == created.Key);
        mine.ShouldNotBeNull();
        mine!.TenantId.ShouldBeNull();
        mine.Value.ShouldBe("listed-value");
        mine.ValueType.ShouldBe("String");
    }
}
