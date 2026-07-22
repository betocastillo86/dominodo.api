using Dominodo.Users.Domain.Roles;
using PermissionCodes = Dominodo.Shared.Kernel.Authorization.Permissions;

namespace Dominodo.Users.Persistence.Seed;

// Global RBAC catalog + bootstrap SuperAdmin, applied via EF HasData (deterministic values only).
public static class UsersSeedData
{
    // Fixed bootstrap SuperAdmin identity (stable across migrations).
    public static readonly Guid SuperAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string SuperAdminPhone = "+1111111";
    public const string SuperAdminEmail = "superadmin@dominodo.local";
    
    // Pre-computed BCrypt hash of the bootstrap password "123456" (workFactor 11).
    // Hardcoded so HasData stays deterministic (BCrypt salts are random per call).
    public const string SuperAdminPasswordHash = "$2b$11$3qiA6Ogz7cU0k/slUmOy5uiFJOvKStCMp0VjaMPqiw7ry8PxOm71i";

    // Role ids
    public const int SuperAdminRoleId = 1;
    public const int AdministradorRoleId = 2;
    public const int AsistenteAdministracionRoleId = 3;
    public const int VigilanteRoleId = 4;
    public const int ResidenteRoleId = 5;

    // Fixed id for the seeded SuperAdmin PlatformRoleAssignment row.
    public static readonly Guid SuperAdminPlatformAssignmentId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    public static IReadOnlyList<Role> Roles { get; } = new List<Role>
    {
        new(SuperAdminRoleId,              "SuperAdmin",              "Acceso total a la plataforma (cross-tenant).",   isSystem: true, RoleScope.Platform),
        new(AdministradorRoleId,           "Administrador",           "Administra un conjunto residencial.",            isSystem: true, RoleScope.Tenant),
        new(AsistenteAdministracionRoleId, "AsistenteAdministracion", "Asiste en la administración del conjunto.",      isSystem: true, RoleScope.Tenant),
        new(VigilanteRoleId,               "Vigilante",               "Personal de portería y seguridad.",              isSystem: true, RoleScope.Tenant),
        new(ResidenteRoleId,               "Residente",               "Residente de un apartamento.",                   isSystem: true, RoleScope.Tenant)
    };

    public static IReadOnlyList<Permission> Permissions { get; } = new List<Permission>
    {
        new(1,  PermissionCodes.UsersManage,         "Gestionar usuarios.",                   "Usuarios"),
        new(2,  PermissionCodes.RolesManage,         "Gestionar roles y permisos.",           "Usuarios"),
        new(9,  PermissionCodes.SettingsView,        "Ver configuración.",                    "Administración"),
        new(10, PermissionCodes.TenantsCreate,       "Crear conjuntos residenciales.",        "Plataforma"),
        new(11, PermissionCodes.TenantsView,         "Ver conjuntos residenciales.",          "Plataforma"),
        new(12, PermissionCodes.TenantsEdit,         "Editar conjuntos residenciales.",       "Plataforma"),
        new(13, PermissionCodes.MembershipsManage,   "Gestionar membresías de un conjunto.",  "Membresías"),
        new(14, PermissionCodes.ApartmentsCreate,    "Crear apartamentos.",                    "Apartamentos"),
        new(15, PermissionCodes.ApartmentsView,      "Ver apartamentos.",                      "Apartamentos"),
        new(16, PermissionCodes.ApartmentsEdit,      "Editar apartamentos.",                   "Apartamentos"),
        new(17, PermissionCodes.SettingsCreate,      "Crear configuración.",                  "Administración"),
        new(18, PermissionCodes.SettingsEdit,        "Editar configuración.",                 "Administración"),
        new(19, PermissionCodes.NotificationsView,   "Ver notificaciones.",                   "Notificaciones"),
        new(20, PermissionCodes.NotificationsCreate, "Crear notificaciones.",                 "Notificaciones"),
        new(21, PermissionCodes.NotificationsEdit,   "Editar notificaciones.",                "Notificaciones"),
        new(22, PermissionCodes.UsersView,           "Ver usuarios.",                          "Usuarios"),
        // Operations — granular catalog (replaces the coarse ids 3–8; appended as 23–35 to keep the
        // E2E fixture-id template …10{Id:D2} 2-digit-safe and unrelated ids stable).
        new(23, PermissionCodes.RequestsView,        "Ver solicitudes (PQRS).",               "Solicitudes"),
        new(24, PermissionCodes.RequestsEdit,        "Editar solicitudes (PQRS).",            "Solicitudes"),
        new(25, PermissionCodes.RequestsManage,      "Gestionar el ciclo de vida de solicitudes (PQRS).", "Solicitudes"),
        new(26, PermissionCodes.RequestsDelete,      "Eliminar o cancelar solicitudes (PQRS).", "Solicitudes"),
        new(27, PermissionCodes.DeliveriesView,      "Ver paquetería.",                       "Paquetería"),
        new(28, PermissionCodes.DeliveriesEdit,      "Editar y cambiar el estado de paquetería.", "Paquetería"),
        new(29, PermissionCodes.DeliveriesCreate,    "Registrar paquetería.",                 "Paquetería"),
        new(30, PermissionCodes.VisitsView,          "Ver visitas.",                          "Visitas"),
        new(31, PermissionCodes.VisitsEdit,          "Editar y cambiar el estado de visitas.", "Visitas"),
        new(32, PermissionCodes.VisitsCreate,        "Registrar visitas.",                    "Visitas"),
        new(33, PermissionCodes.AnnouncementsView,   "Ver comunicados (incl. borradores).",   "Comunicaciones"),
        new(34, PermissionCodes.AnnouncementsEdit,   "Editar, publicar y archivar comunicados.", "Comunicaciones"),
        new(35, PermissionCodes.AnnouncementsCreate, "Crear borradores de comunicados.",      "Comunicaciones"),
        new(36, PermissionCodes.UsersEdit,           "Editar usuarios.",                      "Usuarios")
    };

    // SuperAdmin gets every permission; other roles get tenant-scoped grants per their responsibilities.
    public static IReadOnlyList<RolePermission> RolePermissions { get; } =
        Permissions.Select(p => new RolePermission(SuperAdminRoleId, p.Id))
            // Administrador: memberships + settings (view/create/edit) + notifications (view/create/edit)
            .Append(new RolePermission(AdministradorRoleId, 13))
            .Append(new RolePermission(AdministradorRoleId, 9))
            .Append(new RolePermission(AdministradorRoleId, 17))
            .Append(new RolePermission(AdministradorRoleId, 18))
            .Append(new RolePermission(AdministradorRoleId, 19))
            .Append(new RolePermission(AdministradorRoleId, 20))
            .Append(new RolePermission(AdministradorRoleId, 21))
            // Administrador: all 13 Operations permissions (23–35)
            .Append(new RolePermission(AdministradorRoleId, 23))
            .Append(new RolePermission(AdministradorRoleId, 24))
            .Append(new RolePermission(AdministradorRoleId, 25))
            .Append(new RolePermission(AdministradorRoleId, 26))
            .Append(new RolePermission(AdministradorRoleId, 27))
            .Append(new RolePermission(AdministradorRoleId, 28))
            .Append(new RolePermission(AdministradorRoleId, 29))
            .Append(new RolePermission(AdministradorRoleId, 30))
            .Append(new RolePermission(AdministradorRoleId, 31))
            .Append(new RolePermission(AdministradorRoleId, 32))
            .Append(new RolePermission(AdministradorRoleId, 33))
            .Append(new RolePermission(AdministradorRoleId, 34))
            .Append(new RolePermission(AdministradorRoleId, 35))
            // AsistenteAdministracion: notifications view + create
            .Append(new RolePermission(AsistenteAdministracionRoleId, 19))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 20))
            // AsistenteAdministracion: all Operations except requests.delete (26)
            .Append(new RolePermission(AsistenteAdministracionRoleId, 23))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 24))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 25))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 27))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 28))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 29))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 30))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 31))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 32))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 33))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 34))
            .Append(new RolePermission(AsistenteAdministracionRoleId, 35))
            // Vigilante: deliveries.{view,edit,create} (27–29) + visits.{view,edit,create} (30–32)
            .Append(new RolePermission(VigilanteRoleId, 27))
            .Append(new RolePermission(VigilanteRoleId, 28))
            .Append(new RolePermission(VigilanteRoleId, 29))
            .Append(new RolePermission(VigilanteRoleId, 30))
            .Append(new RolePermission(VigilanteRoleId, 31))
            .Append(new RolePermission(VigilanteRoleId, 32))
            // Residente: no permission grants — relies on ownership + membership-gated create + /mine
            .ToList();

    // The bootstrap SuperAdmin is assigned the Platform-scope SuperAdmin role via data, not code.
    public static IReadOnlyList<PlatformRoleAssignment> PlatformRoleAssignments { get; } = new List<PlatformRoleAssignment>
    {
        PlatformRoleAssignment.AssignWithId(SuperAdminPlatformAssignmentId, SuperAdminUserId, SuperAdminRoleId)
    };
}
