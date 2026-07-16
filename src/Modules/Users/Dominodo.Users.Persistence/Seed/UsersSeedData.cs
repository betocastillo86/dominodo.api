using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Domain.Users;

namespace Dominodo.Users.Persistence.Seed;

// Global RBAC catalog + bootstrap SuperAdmin, applied via EF HasData (deterministic values only).
public static class UsersSeedData
{
    // Fixed bootstrap SuperAdmin identity (stable across migrations).
    public static readonly Guid SuperAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string SuperAdminPhone = "+573000000001";
    public const string SuperAdminEmail = "superadmin@dominodo.local";

    // Pre-computed BCrypt hash of the bootstrap password "SuperAdmin123*" (workFactor 11).
    // Hardcoded so HasData stays deterministic (BCrypt salts are random per call).
    public const string SuperAdminPasswordHash = "$2a$11$F4OymXd2ZrDSdu9WIJDuQumacTFvI2uzij0pELIuDLLsQGJITBnb6";

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
        new(1,  "users.manage",          "Gestionar usuarios.",                   "Usuarios"),
        new(2,  "roles.manage",          "Gestionar roles y permisos.",           "Usuarios"),
        new(3,  "requests.create",       "Crear solicitudes (PQRS).",             "Solicitudes"),
        new(4,  "requests.manage",       "Gestionar solicitudes (PQRS).",         "Solicitudes"),
        new(5,  "deliveries.register",   "Registrar paquetería.",                 "Paquetería"),
        new(6,  "deliveries.manage",     "Gestionar paquetería.",                 "Paquetería"),
        new(7,  "visits.register",       "Registrar visitas.",                    "Visitas"),
        new(8,  "announcements.manage",  "Gestionar boletines.",                  "Comunicaciones"),
        new(9,  "settings.manage",       "Gestionar configuración.",              "Administración"),
        new(10, "tenants.create",        "Crear conjuntos residenciales.",        "Plataforma"),
        new(11, "tenants.manage",        "Gestionar conjuntos residenciales.",    "Plataforma")
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
