namespace Dominodo.E2E.Clients.Modules.Admin.Models;

/// <summary>
/// Hand-replicated mirror of the API's <c>NotificationType</c> enum (int values as stored in
/// <c>admin.NotificationTemplates.Type</c>). Used to seed rows via the dev-only SQL endpoint and to assert
/// the DTO's <c>Type</c> name. Kept in sync manually with
/// <c>src/Modules/Admin/Dominodo.Admin.Domain/Notifications/NotificationType.cs</c>.
/// </summary>
public static class NotificationTemplateTypes
{
    public const int Welcome = 0;
    public const int RequestOpened = 1;
    public const int RequestUpdated = 2;
    public const int RequestClosed = 3;
    public const int DeliveryReceived = 4;
    public const int VisitRegistered = 5;
    public const int Announcement = 6;
}
