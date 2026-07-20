namespace Dominodo.Shared.Kernel.Authorization;

// Single source of truth for permission codes. The Users seed maps roles to these codes and
// controllers authorize on them via [HasPermission(Permissions.X)] — never a raw string literal.
// A code here MUST have a matching seeded Permission (and vice-versa). See
// docs/architecture/12-permission-authorization.md.
public static class Permissions
{
    // Usuarios
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";

    // Solicitudes (PQRS)
    public const string RequestsCreate = "requests.create";
    public const string RequestsManage = "requests.manage";

    // Paquetería
    public const string DeliveriesRegister = "deliveries.register";
    public const string DeliveriesManage = "deliveries.manage";

    // Visitas
    public const string VisitsRegister = "visits.register";

    // Comunicaciones
    public const string AnnouncementsManage = "announcements.manage";

    // Administración
    public const string SettingsManage = "settings.manage";

    // Plataforma / Conjuntos (tenant-independent)
    public const string TenantsCreate = "tenants.create";  // create a new conjunto
    public const string TenantsView = "tenants.view";      // read tenants, apartments, residents, features
    public const string TenantsEdit = "tenants.edit";      // write everything under a conjunto (except create)

    // Membresías
    public const string MembershipsManage = "memberships.manage";  // invite/manage members inside a conjunto
}
