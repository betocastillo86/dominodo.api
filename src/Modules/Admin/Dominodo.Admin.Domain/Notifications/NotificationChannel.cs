namespace Dominodo.Admin.Domain.Notifications;

// Delivery channels a template targets (domain-model §4.1). [Flags] — a template may span several.
[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Push = 2,
    InApp = 4
}
