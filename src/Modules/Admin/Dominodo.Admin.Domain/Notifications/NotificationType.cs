namespace Dominodo.Admin.Domain.Notifications;

// The event a template renders for (domain-model §4.1). Only Welcome is needed today; the rest are
// declared so templates and future consumers (§4.5) have stable values to reference — more will follow.
public enum NotificationType
{
    Welcome,
    RequestOpened,
    RequestUpdated,
    RequestClosed,
    DeliveryReceived,
    VisitRegistered,
    Announcement
}
