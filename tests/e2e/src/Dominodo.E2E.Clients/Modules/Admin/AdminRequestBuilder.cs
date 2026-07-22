using System.Text.Json;
using Dominodo.E2E.Clients.Common;
using Dominodo.E2E.Clients.Dev;
using Dominodo.E2E.Clients.Modules.Admin.Models;

namespace Dominodo.E2E.Clients.Modules.Admin;

/// <summary>
/// Builds Admin-module request models (fake but valid data by default) and composes full <c>Arrange</c>
/// use cases. Per README §8, any Arrange helper that calls the API throws on non-success — a broken
/// Arrange aborts the test rather than producing a misleading Assert.
///
/// The Devices controller exposes no GET, so read-back is done through the dev-only SQL endpoint
/// (<see cref="ISqlClient"/>) against <c>admin.DeviceRegistrations</c> — the only way to observe the
/// persisted row without touching <c>src/</c>.
/// </summary>
public sealed class AdminRequestBuilder(IAdminClient admin, ISqlClient sql) : BaseRequestBuilder
{
    private readonly IAdminClient _admin = admin;
    private readonly ISqlClient _sql = sql;

    /// <summary>
    /// Builds a valid <see cref="NewDeviceModel"/> (Platform "Android", a unique device token). Any field
    /// is overridable: <c>model with { Platform = "NotAPlatform" }</c> for the 400 case. Does NOT call the API.
    /// </summary>
    public NewDeviceModel BuildNewDeviceModel(string? platform = null, string? deviceToken = null)
    {
        return new NewDeviceModel
        {
            Platform = platform ?? "Android",
            Token = deviceToken ?? $"e2e-device-{Guid.NewGuid():N}",
        };
    }

    /// <summary>
    /// Full Arrange (parameter overload): builds a valid <see cref="NewDeviceModel"/> from the given
    /// overrides and registers it for the user behind <paramref name="authToken"/>.
    /// </summary>
    public Task<DeviceRow> RegisterDeviceAsync(string authToken, string? platform = null, string? deviceToken = null)
    {
        return RegisterDeviceAsync(authToken, BuildNewDeviceModel(platform, deviceToken));
    }

    /// <summary>
    /// Full Arrange: registers the given device for the user behind <paramref name="authToken"/>, reads the
    /// persisted row back via the dev-only SQL endpoint, and returns it. Throws on any non-success step so a
    /// broken Arrange aborts the test immediately.
    /// </summary>
    public async Task<DeviceRow> RegisterDeviceAsync(string authToken, NewDeviceModel model)
    {
        var response = await _admin.RegisterDevice(model, authToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: registering a device returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        var id = response.Content!.Id;
        var row = await FindDeviceByIdAsync(id);
        if (row is null)
        {
            throw new InvalidOperationException(
                $"Arrange failed: registered device {id} was not found in admin.DeviceRegistrations.");
        }

        return row;
    }

    /// <summary>
    /// Reads a device row back from <c>admin.DeviceRegistrations</c> by id via the dev-only SQL endpoint,
    /// or <c>null</c> if absent — used to assert an endpoint's side effect (the row was linked / deactivated).
    /// Throws on a non-success query.
    /// </summary>
    public Task<DeviceRow?> FindDeviceByIdAsync(Guid id)
    {
        return QueryDeviceAsync($"[Id] = '{id}'");
    }

    /// <summary>
    /// Reads a device row back by its device token via the dev-only SQL endpoint, or <c>null</c> if absent.
    /// Throws on a non-success query.
    /// </summary>
    public Task<DeviceRow?> FindDeviceByTokenAsync(string deviceToken)
    {
        var safeToken = deviceToken.Replace("'", "''");
        return QueryDeviceAsync($"[Token] = '{safeToken}'");
    }

    private async Task<DeviceRow?> QueryDeviceAsync(string whereClause)
    {
        var query =
            "SELECT [Id], [UserId], [Platform], [IsActive] " +
            $"FROM [admin].[DeviceRegistrations] WHERE {whereClause}";

        var response = await _sql.Query(new SqlRequestModel(query));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: dev SQL query returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        var row = response.Content!.Result.FirstOrDefault();
        if (row is null)
        {
            return null;
        }

        return new DeviceRow(
            Id: Guid.Parse(AsString(row["Id"])!),
            UserId: Guid.Parse(AsString(row["UserId"])!),
            Platform: AsString(row["Platform"])!,
            IsActive: AsBool(row["IsActive"]));
    }

    /// <summary>
    /// Builds a valid <see cref="UpdateNotificationTemplateModel"/> (all channels enabled with content, so it
    /// passes the validator by default). Any field is overridable for the 400 cases:
    /// <c>model with { EmailSubject = null }</c>. Does NOT call the API.
    /// </summary>
    public UpdateNotificationTemplateModel BuildUpdateNotificationTemplateModel(
        bool emailEnabled = true,
        bool pushEnabled = true,
        bool inAppEnabled = true,
        string? emailSubject = null,
        string? emailBodyHtml = null,
        string? inAppText = null,
        string? pushText = null,
        bool isActive = true,
        string? localization = null)
    {
        return new UpdateNotificationTemplateModel
        {
            EmailEnabled = emailEnabled,
            PushEnabled = pushEnabled,
            InAppEnabled = inAppEnabled,
            EmailSubject = emailSubject ?? $"e2e-subject-{Guid.NewGuid():N}",
            EmailBodyHtml = emailBodyHtml ?? "<p>e2e body</p>",
            InAppText = inAppText ?? "e2e in-app text",
            PushText = pushText ?? "e2e push text",
            IsActive = isActive,
            Localization = localization,
        };
    }

    /// <summary>
    /// Arrange: seeds a GLOBAL notification template (TenantId NULL) for the given <paramref name="type"/> via
    /// the dev-only SQL endpoint — there is no create-by-API (§4.2), so this is the only way to arrange the
    /// row. Idempotent across re-runs: the row for a scope is unique on (Type, TenantId), so any existing
    /// global row of that type is deleted first. Reads the row back and returns it; throws on any non-success
    /// step so a broken Arrange aborts the test immediately.
    /// </summary>
    public async Task<NotificationTemplateRow> SeedGlobalNotificationTemplateAsync(
        int type,
        bool emailEnabled = true,
        bool pushEnabled = false,
        bool inAppEnabled = true,
        string? emailSubject = "e2e-subject",
        string? emailBodyHtml = "<p>e2e body</p>",
        string? inAppText = "e2e in-app text",
        string? pushText = null,
        bool isActive = true,
        string? localization = null)
    {
        var id = Guid.NewGuid();
        var statement =
            $"DELETE FROM [admin].[NotificationTemplates] WHERE [Type] = {type} AND [TenantId] IS NULL; " +
            "INSERT INTO [admin].[NotificationTemplates] " +
            "([Id],[TenantId],[Type],[EmailEnabled],[PushEnabled],[InAppEnabled]," +
            "[EmailSubject],[EmailBodyHtml],[InAppText],[PushText],[IsActive],[Localization]) VALUES (" +
            $"'{id}', NULL, {type}, {Bit(emailEnabled)}, {Bit(pushEnabled)}, {Bit(inAppEnabled)}, " +
            $"{SqlStr(emailSubject)}, {SqlStr(emailBodyHtml)}, {SqlStr(inAppText)}, {SqlStr(pushText)}, " +
            $"{Bit(isActive)}, {SqlStr(localization)})";

        var response = await _sql.Execute(new SqlRequestModel(statement));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: seeding a notification template returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        var row = await FindNotificationTemplateByIdAsync(id);
        if (row is null)
        {
            throw new InvalidOperationException(
                $"Arrange failed: seeded notification template {id} was not found in admin.NotificationTemplates.");
        }

        return row;
    }

    /// <summary>
    /// Reads a notification-template row back from <c>admin.NotificationTemplates</c> by id via the dev-only
    /// SQL endpoint, or <c>null</c> if absent — used to assert the update's side effect. Throws on a non-success query.
    /// </summary>
    public async Task<NotificationTemplateRow?> FindNotificationTemplateByIdAsync(Guid id)
    {
        var query =
            "SELECT [Id],[TenantId],[Type],[EmailEnabled],[PushEnabled],[InAppEnabled]," +
            "[EmailSubject],[EmailBodyHtml],[InAppText],[PushText],[IsActive],[Localization] " +
            $"FROM [admin].[NotificationTemplates] WHERE [Id] = '{id}'";

        var response = await _sql.Query(new SqlRequestModel(query));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Read-back failed: dev SQL query returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        var row = response.Content!.Result.FirstOrDefault();
        if (row is null)
        {
            return null;
        }

        return new NotificationTemplateRow(
            Id: Guid.Parse(AsString(row["Id"])!),
            TenantId: AsString(row["TenantId"]) is { } t ? Guid.Parse(t) : null,
            Type: AsInt(row["Type"]),
            EmailEnabled: AsBool(row["EmailEnabled"]),
            PushEnabled: AsBool(row["PushEnabled"]),
            InAppEnabled: AsBool(row["InAppEnabled"]),
            EmailSubject: AsString(row["EmailSubject"]),
            EmailBodyHtml: AsString(row["EmailBodyHtml"]),
            InAppText: AsString(row["InAppText"]),
            PushText: AsString(row["PushText"]),
            IsActive: AsBool(row["IsActive"]),
            Localization: AsString(row["Localization"]));
    }

    // NULL for a null value, else a single-quoted literal with embedded quotes doubled.
    private static string SqlStr(string? value)
    {
        return value is null ? "NULL" : $"'{value.Replace("'", "''")}'";
    }

    private static int Bit(bool value)
    {
        return value ? 1 : 0;
    }

    // Dev SQL values deserialize as JsonElement (object). SQL Server bit → JSON true/false;
    // uniqueidentifier/nvarchar → JSON string.
    private static string? AsString(object? value)
    {
        return value is JsonElement element
            ? element.ValueKind == JsonValueKind.Null ? null : element.ToString()
            : value?.ToString();
    }

    private static bool AsBool(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => element.GetInt32() != 0,
                JsonValueKind.String => bool.Parse(element.GetString()!),
                _ => false,
            };
        }

        return Convert.ToBoolean(value);
    }

    private static int AsInt(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetInt32(),
                JsonValueKind.String => int.Parse(element.GetString()!),
                _ => 0,
            };
        }

        return Convert.ToInt32(value);
    }
}

/// <summary>A persisted <c>admin.DeviceRegistrations</c> row, read back for side-effect assertions.</summary>
public sealed record DeviceRow(Guid Id, Guid UserId, string Platform, bool IsActive);

/// <summary>A persisted <c>admin.NotificationTemplates</c> row, read back for Arrange and side-effect assertions.</summary>
public sealed record NotificationTemplateRow(
    Guid Id,
    Guid? TenantId,
    int Type,
    bool EmailEnabled,
    bool PushEnabled,
    bool InAppEnabled,
    string? EmailSubject,
    string? EmailBodyHtml,
    string? InAppText,
    string? PushText,
    bool IsActive,
    string? Localization);
