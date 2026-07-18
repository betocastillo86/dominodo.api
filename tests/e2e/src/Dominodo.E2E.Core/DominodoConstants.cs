namespace Dominodo.E2E.Core;

public static class DominodoConstants
{
    public static class Headers
    {
        // Must match src/Shared/.../Multitenancy/TenantHeaders.Name
        public const string Tenant = "X-Tenant";

        // Must match src/Shared/.../Http/CorrelationIdMiddleware
        public const string CorrelationId = "X-Correlation-Id";

        public const string TestName = "X-TestName";
    }

    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
    }

    // Mirror of Shared.Kernel.Authorization.Permissions — kept in sync manually.
    // Use these instead of raw string literals in token factories and test assertions.
    public static class Permission
    {
        public const string UsersManage         = "users.manage";
        public const string RolesManage         = "roles.manage";
        public const string RequestsCreate      = "requests.create";
        public const string RequestsManage      = "requests.manage";
        public const string DeliveriesRegister  = "deliveries.register";
        public const string DeliveriesManage    = "deliveries.manage";
        public const string VisitsRegister      = "visits.register";
        public const string AnnouncementsManage = "announcements.manage";
        public const string SettingsManage      = "settings.manage";
        public const string TenantsCreate       = "tenants.create";
        public const string TenantsManage       = "tenants.manage";
    }

    public static class Defaults
    {
        // Present but unused while multitenancy is deferred (NullTenantDirectory).
        public const string TenantSlug = "e2e-default";
    }

    // Fixed identities seeded by the API under the IntegrationTests environment: one Platform role +
    // user per permission, each user carrying exactly that permission. Mint a token via
    // JwtTokenFactory.GenerateToken(permission) and hit the matching [HasPermission(code)] endpoint
    // to assert authorization — the server resolves the permission set from the DB by the token sub.
    // Must match src/Modules/Users/Dominodo.Users.Persistence/Seed/IntegrationTestSeedData.cs
    // (UserId = 00000000-0000-0000-0000-0000000010NN, NN = permission id).
    public static class IntegrationSeed
    {
        private static readonly Dictionary<string, Guid> _userIdByPermission = new()
        {
            ["users.manage"]         = Guid.Parse("00000000-0000-0000-0000-000000001001"),
            ["roles.manage"]         = Guid.Parse("00000000-0000-0000-0000-000000001002"),
            ["requests.create"]      = Guid.Parse("00000000-0000-0000-0000-000000001003"),
            ["requests.manage"]      = Guid.Parse("00000000-0000-0000-0000-000000001004"),
            ["deliveries.register"]  = Guid.Parse("00000000-0000-0000-0000-000000001005"),
            ["deliveries.manage"]    = Guid.Parse("00000000-0000-0000-0000-000000001006"),
            ["visits.register"]      = Guid.Parse("00000000-0000-0000-0000-000000001007"),
            ["announcements.manage"] = Guid.Parse("00000000-0000-0000-0000-000000001008"),
            ["settings.manage"]      = Guid.Parse("00000000-0000-0000-0000-000000001009"),
            ["tenants.create"]       = Guid.Parse("00000000-0000-0000-0000-000000001010"),
            ["tenants.manage"]       = Guid.Parse("00000000-0000-0000-0000-000000001011"),
        };

        public static Guid UserIdFor(string permission) =>
            _userIdByPermission.TryGetValue(permission, out var id)
                ? id
                : throw new ArgumentException($"No seeded user for permission '{permission}'.", nameof(permission));
    }
}
