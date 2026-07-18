using System.Globalization;
using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Domain.Users;
using Dominodo.Users.Domain.ValueObjects;

namespace Dominodo.Users.Persistence.Seed;

// IntegrationTests-only fixtures (applied at runtime by SeedIntegrationTestDataAsync, never via HasData).
// For every permission in the catalog we materialise:
//   • a Platform-scope Role carrying exactly that one permission,
//   • a User with a FIXED, deterministic Guid,
//   • a PlatformRoleAssignment linking them,
// so a test can mint a token for the fixed user id and hit a [HasPermission(code)] endpoint expecting 200.
// Ids are derived from the permission id so they are stable and easy to reference from tests.
public static class IntegrationTestSeedData
{
    // A single seeded fixture: the permission it grants and the fixed ids the test can rely on.
    public sealed record Fixture(
        string PermissionCode,
        int PermissionId,
        int RoleId,
        string RoleName,
        Guid UserId,
        Guid AssignmentId);

    // Derived from UsersSeedData.Permissions so codes/ids never drift. Deterministic id scheme:
    //   RoleId       = 1000 + permissionId              (1001..; clear of system roles 1-5)
    //   UserId       = 00000000-0000-0000-0000-0000000010NN   (NN = permissionId, 2 digits)
    //   AssignmentId = 00000000-0000-0000-0000-0000000020NN
    public static IReadOnlyList<Fixture> Fixtures { get; } = UsersSeedData.Permissions
        .Select(p => new Fixture(
            PermissionCode: p.Code,
            PermissionId: p.Id,
            RoleId: 1000 + p.Id,
            RoleName: ToPascalCase(p.Code),
            UserId: Guid.Parse($"00000000-0000-0000-0000-0000000010{p.Id:D2}"),
            AssignmentId: Guid.Parse($"00000000-0000-0000-0000-0000000020{p.Id:D2}")))
        .ToList();

    // Builds the Role aggregate (Platform scope, one permission) for a fixture.
    public static Role BuildRole(Fixture fixture)
    {
        var role = new Role(
            fixture.RoleId,
            fixture.RoleName,
            $"Integration test role granting {fixture.PermissionCode}.",
            isSystem: false,
            RoleScope.Platform);

        role.AssignPermissions([fixture.PermissionId]);
        return role;
    }

    // Builds the User aggregate for a fixture. Reuses the known bootstrap password hash ("123456")
    // so login-based tests work too; phone is a unique E.164 value derived from the permission id.
    public static User BuildUser(Fixture fixture)
    {
        var phone = PhoneNumber.Create($"+9900000{fixture.PermissionId:D3}").Value;
        var email = Email.Create($"{fixture.PermissionCode}@integration.test").Value;

        return User.CreateSeed(
            fixture.UserId,
            phone,
            email,
            firstName: "Integration",
            lastName: fixture.RoleName,
            passwordHash: UsersSeedData.SuperAdminPasswordHash);
    }

    public static PlatformRoleAssignment BuildAssignment(Fixture fixture) =>
        PlatformRoleAssignment.AssignWithId(fixture.AssignmentId, fixture.UserId, fixture.RoleId);

    // "Rol Public": a Platform-scope role carrying ZERO permissions, plus a user assigned to it, so a test
    // can mint a token for a permission-less user and assert a [HasPermission(code)] endpoint returns 403.
    // Fixed ids chosen clear of every other scheme (system roles 1-5, per-permission roles 1001+, their
    // users ...10NN / assignments ...20NN). Uses NN=00 slots, which the per-permission scheme never emits.
    public const int PublicRoleId = 900;
    public const string PublicRoleName = "Rol Public";
    public static readonly Guid PublicUserId = Guid.Parse("00000000-0000-0000-0000-000000001000");
    public static readonly Guid PublicAssignmentId = Guid.Parse("00000000-0000-0000-0000-000000002000");

    public static Role BuildPublicRole() =>
        new(
            PublicRoleId,
            PublicRoleName,
            "Integration test role with no permissions.",
            isSystem: false,
            RoleScope.Platform);

    public static User BuildPublicUser()
    {
        var phone = PhoneNumber.Create("+9900000900").Value;
        var email = Email.Create("public@integration.test").Value;

        return User.CreateSeed(
            PublicUserId,
            phone,
            email,
            firstName: "Integration",
            lastName: "Public",
            passwordHash: UsersSeedData.SuperAdminPasswordHash);
    }

    public static PlatformRoleAssignment BuildPublicAssignment() =>
        PlatformRoleAssignment.AssignWithId(PublicAssignmentId, PublicUserId, PublicRoleId);

    // "roles.manage" -> "RolesManage", "deliveries.register" -> "DeliveriesRegister".
    private static string ToPascalCase(string permissionCode)
    {
        var segments = permissionCode.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(segments.Select(s =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant())));
    }
}
