using System.Globalization;
using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Domain.Users;
using Dominodo.Users.Domain.ValueObjects;

namespace Dominodo.Users.Persistence.Seed;

// IntegrationTests-only fixtures (applied at runtime by SeedIntegrationTestDataAsync, never via HasData).
// For every permission in the catalog we materialise TWO parallel identities so both branches of
// permission resolution (doc 12) can be exercised:
//   • Platform fixture — a Platform-scope Role carrying that one permission, a User, and a
//     PlatformRoleAssignment. Mint a token for the user id and hit [HasPermission(code)] WITHOUT a tenant.
//   • Tenant fixture — a Tenant-scope Role carrying that one permission, a User, and an Active Membership
//     in the fixed integration-test tenant. Mint a token for the user id, send X-Tenant: <IntegrationTenantSlug>,
//     and hit [HasPermission(code)] — the permission resolves only via the tenant branch.
// Ids are derived from the permission id so they are stable and easy to reference from tests.
public static class IntegrationTestSeedData
{
    // Fixed Active tenant that the Tenant fixtures' memberships belong to. The Tenants module seeds a
    // matching Tenant row (same id + slug) so X-Tenant resolves it; here we only need the raw Guid
    // (Tenant lives in another module — no cross-boundary FK, rule #2).
    public static readonly Guid IntegrationTenantId = Guid.Parse("00000000-0000-0000-0000-0000000000E2");
    public const string IntegrationTenantSlug = "integration-test";

    // A stable timestamp for seeded memberships/joins (Active from a fixed point in time).
    private static readonly DateTimeOffset SeedInstant = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // A single Platform seeded fixture: the permission it grants and the fixed ids the test can rely on.
    public sealed record Fixture(
        string PermissionCode,
        int PermissionId,
        int RoleId,
        string RoleName,
        Guid UserId,
        Guid AssignmentId);

    // A single Tenant seeded fixture: a Tenant-scope role + user + Active membership in IntegrationTenantId.
    public sealed record TenantFixture(
        string PermissionCode,
        int PermissionId,
        int RoleId,
        string RoleName,
        Guid UserId,
        Guid MembershipId);

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

    // The Tenant-scope counterpart of Fixtures. Distinct id scheme so it never collides with the platform
    // fixtures (or system roles 1-5):
    //   RoleId       = 2000 + permissionId              (2001..)
    //   UserId       = 00000000-0000-0000-0000-0000000011NN   (NN = permissionId, 2 digits)
    //   MembershipId = 00000000-0000-0000-0000-0000000031NN
    public static IReadOnlyList<TenantFixture> TenantFixtures { get; } = UsersSeedData.Permissions
        .Select(p => new TenantFixture(
            PermissionCode: p.Code,
            PermissionId: p.Id,
            RoleId: 2000 + p.Id,
            RoleName: $"{ToPascalCase(p.Code)}Tenant",
            UserId: Guid.Parse($"00000000-0000-0000-0000-0000000011{p.Id:D2}"),
            MembershipId: Guid.Parse($"00000000-0000-0000-0000-0000000031{p.Id:D2}")))
        .ToList();

    // Builds the Role aggregate (Platform scope, one permission) for a fixture.
    public static Role BuildRole(Fixture fixture)
    {
        var role = new Role(
            fixture.RoleId,
            fixture.RoleName,
            $"Integration test platform role granting {fixture.PermissionCode}.",
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

    // Builds the Role aggregate (Tenant scope, one permission) for a tenant fixture.
    public static Role BuildTenantRole(TenantFixture fixture)
    {
        var role = new Role(
            fixture.RoleId,
            fixture.RoleName,
            $"Integration test tenant role granting {fixture.PermissionCode}.",
            isSystem: false,
            RoleScope.Tenant);

        role.AssignPermissions([fixture.PermissionId]);
        return role;
    }

    // Builds the User for a tenant fixture. Phone/email are offset from the platform scheme (+99001…)
    // to stay unique across the two identities per permission.
    public static User BuildTenantUser(TenantFixture fixture)
    {
        var phone = PhoneNumber.Create($"+9900100{fixture.PermissionId:D3}").Value;
        var email = Email.Create($"{fixture.PermissionCode}@tenant.integration.test").Value;

        return User.CreateSeed(
            fixture.UserId,
            phone,
            email,
            firstName: "Integration",
            lastName: fixture.RoleName,
            passwordHash: UsersSeedData.SuperAdminPasswordHash);
    }

    // An Active membership binding the tenant user to its tenant role inside IntegrationTenantId.
    public static Membership BuildMembership(TenantFixture fixture) =>
        Membership.CreateSeed(fixture.MembershipId, fixture.UserId, IntegrationTenantId, fixture.RoleId, SeedInstant);

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
