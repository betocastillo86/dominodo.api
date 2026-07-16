namespace Dominodo.Users.Domain.Roles;

// Join between Role and Permission (domain-model §1.4). Unique (RoleId, PermissionId).
public sealed class RolePermission
{
    private RolePermission() { } // EF Core

    public RolePermission(int roleId, int permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public int RoleId { get; private set; }
    public int PermissionId { get; private set; }
}
