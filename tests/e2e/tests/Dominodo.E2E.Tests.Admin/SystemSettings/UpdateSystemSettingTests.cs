using System.Net;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.SystemSettings;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/system-settings/{key}</c> (SystemSettingsController.Update), guarded
/// by <c>[HasPermission(Permissions.SettingsEdit)]</c>. Targets the current scope's row (global with no
/// X-Tenant). Anonymous ⇒ 401; a bearer lacking the permission ⇒ 403; an invalid body ⇒ 400
/// Validation.Failed; an unknown key ⇒ 404 SystemSetting.NotFound; success is 204 NoContent and the new
/// value is then observable via GET /{key}. (Update has no duplicate-key path, so the create-only "repeated
/// setting" case is replaced here by the natural 404.)
/// </summary>
[TestFixture]
public sealed class UpdateSystemSettingTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = AdminRequestBuilder.BuildUpdateSystemSettingModel();

        // Act — no token
        var response = await AdminClient.UpdateSystemSetting($"e2e-setting-{Guid.NewGuid():N}", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksSettingsEdit()
    {
        // Arrange — a real, existing user with ZERO permissions. Proves editing a setting needs settings.edit.
        var model = AdminRequestBuilder.BuildUpdateSystemSettingModel();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.UpdateSystemSetting($"e2e-setting-{Guid.NewGuid():N}", model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenBodyIsInvalid()
    {
        // Arrange — break the FluentValidation rule evaluated once the body binds: Value null. (Key is
        // bound from the route; ValueType is an enum rejected at JSON binding — covered separately below.)
        var model = AdminRequestBuilder.BuildUpdateSystemSettingModel() with
        {
            Value = null,
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsEdit);

        // Act
        var response = await AdminClient.UpdateSystemSetting($"e2e-setting-{Guid.NewGuid():N}", model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateSystemSettingModel.Value));
    }

    [Test]
    public async Task _400_WhenValueTypeIsNotAValidEnum()
    {
        // Arrange — an unknown enum name fails at JSON binding (JsonStringEnumConverter); the
        // InvalidModelStateResponseFactory maps it to the same Validation.Failed shape as a validator error.
        var model = AdminRequestBuilder.BuildUpdateSystemSettingModel(valueType: "NotAType");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsEdit);

        // Act
        var response = await AdminClient.UpdateSystemSetting($"e2e-setting-{Guid.NewGuid():N}", model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateSystemSettingModel.ValueType));
    }

    [Test]
    public async Task _404_WhenSettingDoesNotExist()
    {
        // Arrange — a valid body so validation passes and the handler's not-found path is reached.
        var model = AdminRequestBuilder.BuildUpdateSystemSettingModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsEdit);

        // Act
        var response = await AdminClient.UpdateSystemSetting($"e2e-missing-{Guid.NewGuid():N}", model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("SystemSetting.NotFound");
    }

    [Test]
    public async Task _204_UpdatesSetting_AndVerifiesByFetching()
    {
        // Arrange — an existing global setting to update. (The prompt called this "200 verificar guardado";
        // the API returns 204 NoContent, and the new value is verified via GET /{key}.)
        var existing = await AdminRequestBuilder.CreateSystemSettingAsync(value: "before", valueType: "String");
        var model = AdminRequestBuilder.BuildUpdateSystemSettingModel(value: "after", valueType: "Int");
        var editToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsEdit);

        // Act
        var response = await AdminClient.UpdateSystemSetting(existing.Key!, model, token: editToken);

        // Assert — 204 NoContent (no body)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify round-trip: the row now holds the new value and type.
        var viewToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsView);
        var getResponse = await AdminClient.GetSystemSettingByKey(existing.Key!, token: viewToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var saved = getResponse.Content;
        saved.ShouldNotBeNull();
        saved!.Value.ShouldBe("after");
        saved.ValueType.ShouldBe("Int");
    }
}
