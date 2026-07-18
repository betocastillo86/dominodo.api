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
        new(3,  PermissionCodes.RequestsCreate,      "Crear solicitudes (PQRS).",             "Solicitudes"),
        new(4,  PermissionCodes.RequestsManage,      "Gestionar solicitudes (PQRS).",         "Solicitudes"),
        new(5,  PermissionCodes.DeliveriesRegister,  "Registrar paquetería.",                 "Paquetería"),
        new(6,  PermissionCodes.DeliveriesManage,    "Gestionar paquetería.",                 "Paquetería"),
        new(7,  PermissionCodes.VisitsRegister,      "Registrar visitas.",                    "Visitas"),
        new(8,  PermissionCodes.AnnouncementsManage, "Gestionar boletines.",                  "Comunicaciones"),
        new(9,  PermissionCodes.SettingsManage,      "Gestionar configuración.",              "Administración"),
        new(10, PermissionCodes.TenantsCreate,       "Crear conjuntos residenciales.",        "Plataforma"),
        new(11, PermissionCodes.TenantsView,         "Ver conjuntos residenciales.",          "Plataforma"),
        new(12, PermissionCodes.TenantsEdit,         "Editar conjuntos residenciales.",       "Plataforma")
    };

    // SuperAdmin gets every permission.
    public static IReadOnlyList<RolePermission> RolePermissions { get; } =
        Permissions.Select(p => new RolePermission(SuperAdminRoleId, p.Id)).ToList();

    // The bootstrap SuperAdmin is assigned the Platform-scope SuperAdmin role via data, not code.
    public static IReadOnlyList<PlatformRoleAssignment> PlatformRoleAssignments { get; } = new List<PlatformRoleAssignment>
    {
        PlatformRoleAssignment.AssignWithId(SuperAdminPlatformAssignmentId, SuperAdminUserId, SuperAdminRoleId)
    };
}
