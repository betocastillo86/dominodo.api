using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.SystemSettings;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/system-settings/{key}</c> (SystemSettingsController.GetByKey),
/// guarded by <c>[HasPermission(Permissions.SettingsView)]</c>. Resolves the setting for the current scope
/// (the global value with no X-Tenant). Anonymous ⇒ 401; a bearer lacking the permission ⇒ 403; an unknown
/// key ⇒ 404 SystemSetting.NotFound; success is 200 with the resolved value.
/// </summary>
[TestFixture]
public sealed class GetSystemSettingByKeyTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await AdminClient.GetSystemSettingByKey($"e2e-setting-{Guid.NewGuid():N}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksSettingsView()
    {
        // Arrange — a real, existing user with ZERO permissions.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.GetSystemSettingByKey($"e2e-setting-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _404_WhenSettingDoesNotExist()
    {
        // Arrange — a valid bearer so the request reaches the handler's not-found path.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsView);

        // Act
        var response = await AdminClient.GetSystemSettingByKey($"e2e-missing-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("SystemSetting.NotFound");
    }

    [Test]
    public async Task _200_ReturnsSetting()
    {
        // Arrange — an existing global setting.
        var created = await AdminRequestBuilder.CreateSystemSettingAsync(value: "resolved-value", valueType: "String");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsView);

        // Act
        var response = await AdminClient.GetSystemSettingByKey(created.Key!, token: token);

        // Assert — 200 with the resolved global value.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var setting = response.Content;
        setting.ShouldNotBeNull();
        setting!.Key.ShouldBe(created.Key);
        setting.TenantId.ShouldBeNull();
        setting.Value.ShouldBe("resolved-value");
        setting.ValueType.ShouldBe("String");
    }
}
