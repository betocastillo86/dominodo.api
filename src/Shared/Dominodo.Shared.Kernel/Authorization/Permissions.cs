namespace Dominodo.Shared.Kernel.Authorization;

// Single source of truth for permission codes. The Users seed maps roles to these codes and
// controllers authorize on them via [HasPermission(Permissions.X)] — never a raw string literal.
// A code here MUST have a matching seeded Permission (and vice-versa). See
// docs/architecture/12-permission-authorization.md.
public static class Permissions
{
    // Usuarios
    public const string UsersManage = "users.manage";
    public const string UsersView   = "users.view";
    public const string RolesManage = "roles.manage";

    // Solicitudes (PQRS)
    public const string RequestsView   = "requests.view";
    public const string RequestsEdit   = "requests.edit";
    public const string RequestsManage = "requests.manage";
    public const string RequestsDelete = "requests.delete";

    // Paquetería
    public const string DeliveriesView   = "deliveries.view";
    public const string DeliveriesEdit   = "deliveries.edit";
    public const string DeliveriesCreate = "deliveries.create";

    // Visitas
    public const string VisitsView   = "visits.view";
    public const string VisitsEdit   = "visits.edit";
    public const string VisitsCreate = "visits.create";

    // Comunicaciones
    public const string AnnouncementsView   = "announcements.view";
    public const string AnnouncementsEdit   = "announcements.edit";
    public const string AnnouncementsCreate = "announcements.create";

    // Administración
    public const string SettingsView   = "settings.view";
    public const string SettingsCreate = "settings.create";
    public const string SettingsEdit   = "settings.edit";

    // Notificaciones
    public const string NotificationsView   = "notifications.view";
    public const string NotificationsCreate = "notifications.create";
    public const string NotificationsEdit   = "notifications.edit";

    // Plataforma / Conjuntos (tenant-independent)
    public const string TenantsCreate = "tenants.create";  // create a new conjunto
    public const string TenantsView = "tenants.view";      // read tenants, apartments, residents, features
    public const string TenantsEdit = "tenants.edit";      // write everything under a conjunto (except create)

    // Membresías
    public const string MembershipsManage = "memberships.manage";  // invite/manage members inside a conjunto

    // Apartamentos
    public const string ApartmentsCreate = "apartments.create";
    public const string ApartmentsView   = "apartments.view";
    public const string ApartmentsEdit   = "apartments.edit";
}
