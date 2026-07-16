using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Roles;

// Global role catalog keyed by int (domain-model §1.2). Names are unique; system roles cannot be
// deleted. Reference/config data, not a Guid-identified aggregate.
public sealed class Role
{
    private readonly List<RolePermission> _permissions = new();

    private Role() { } // EF Core

    public Role(int id, string name, string? description, bool isSystem, RoleScope scope)
    {
        Id = id;
        Name = name;
        Description = description;
        IsSystem = isSystem;
        Scope = scope;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public RoleScope Scope { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    // Factory for user-created (non-system) roles. Scope is set here and is immutable afterwards.
    public static Result<Role> Create(int id, string name, string? description, RoleScope scope)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Role.NameRequired", "Role name is required.");
        }

        return new Role(id, name.Trim(), description, isSystem: false, scope);
    }

    public Result Rename(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Role.NameRequired", "Role name is required.");
        }

        Name = name.Trim();
        Description = description;
        return Result.Success();
    }

    public void AssignPermissions(IEnumerable<int> permissionIds)
    {
        _permissions.Clear();
        foreach (var permissionId in permissionIds.Distinct())
        {
            _permissions.Add(new RolePermission(Id, permissionId));
        }
    }
}
