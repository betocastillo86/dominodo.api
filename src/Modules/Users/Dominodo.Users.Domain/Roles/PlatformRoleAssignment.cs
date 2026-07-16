using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Roles;

// Represents the assignment of a Platform-scope role to a user (domain-model §1.5).
// Platform authority is data-driven: the bootstrap SuperAdmin gets its role via a seeded row,
// not a hardcoded user-id check.
public sealed class PlatformRoleAssignment : AggregateRoot
{
    private PlatformRoleAssignment() { } // EF Core

    private PlatformRoleAssignment(Guid id, Guid userId, int roleId) : base(id)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }
    public int RoleId { get; private set; }

    public static PlatformRoleAssignment Assign(Guid userId, int roleId) =>
        new(Guid.NewGuid(), userId, roleId);

    // For deterministic seed data only (HasData requires stable Guid values).
    public static PlatformRoleAssignment AssignWithId(Guid id, Guid userId, int roleId) =>
        new(id, userId, roleId);
}
