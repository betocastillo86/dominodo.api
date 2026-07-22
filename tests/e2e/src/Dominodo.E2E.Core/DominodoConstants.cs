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
        public const string UsersManage          = "users.manage";
        public const string RolesManage          = "roles.manage";
        public const string SettingsView         = "settings.view";
        public const string SettingsCreate       = "settings.create";
        public const string SettingsEdit         = "settings.edit";
        public const string NotificationsView    = "notifications.view";
        public const string NotificationsCreate  = "notifications.create";
        public const string NotificationsEdit    = "notifications.edit";
        public const string TenantsCreate        = "tenants.create";
        public const string TenantsView          = "tenants.view";
        public const string TenantsEdit          = "tenants.edit";
        public const string MembershipsManage    = "memberships.manage";
        public const string ApartmentsCreate     = "apartments.create";
        public const string ApartmentsView       = "apartments.view";
        public const string ApartmentsEdit       = "apartments.edit";
        public const string UsersView            = "users.view";
        public const string UsersEdit            = "users.edit";
        // Operations — granular catalog
        public const string RequestsView         = "requests.view";
        public const string RequestsEdit         = "requests.edit";
        public const string RequestsManage       = "requests.manage";
        public const string RequestsDelete       = "requests.delete";
        public const string DeliveriesView       = "deliveries.view";
        public const string DeliveriesEdit       = "deliveries.edit";
        public const string DeliveriesCreate     = "deliveries.create";
        public const string VisitsView           = "visits.view";
        public const string VisitsEdit           = "visits.edit";
        public const string VisitsCreate         = "visits.create";
        public const string AnnouncementsView    = "announcements.view";
        public const string AnnouncementsEdit    = "announcements.edit";
        public const string AnnouncementsCreate  = "announcements.create";
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
            ["settings.view"]        = Guid.Parse("00000000-0000-0000-0000-000000001009"),
            ["tenants.create"]       = Guid.Parse("00000000-0000-0000-0000-000000001010"),
            ["tenants.view"]         = Guid.Parse("00000000-0000-0000-0000-000000001011"),
            ["tenants.edit"]         = Guid.Parse("00000000-0000-0000-0000-000000001012"),
            ["memberships.manage"]   = Guid.Parse("00000000-0000-0000-0000-000000001013"),
            ["apartments.create"]    = Guid.Parse("00000000-0000-0000-0000-000000001014"),
            ["apartments.view"]      = Guid.Parse("00000000-0000-0000-0000-000000001015"),
            ["apartments.edit"]      = Guid.Parse("00000000-0000-0000-0000-000000001016"),
            ["settings.create"]      = Guid.Parse("00000000-0000-0000-0000-000000001017"),
            ["settings.edit"]        = Guid.Parse("00000000-0000-0000-0000-000000001018"),
            ["notifications.view"]   = Guid.Parse("00000000-0000-0000-0000-000000001019"),
            ["notifications.create"] = Guid.Parse("00000000-0000-0000-0000-000000001020"),
            ["notifications.edit"]   = Guid.Parse("00000000-0000-0000-0000-000000001021"),
            ["users.view"]           = Guid.Parse("00000000-0000-0000-0000-000000001022"),
            ["requests.view"]        = Guid.Parse("00000000-0000-0000-0000-000000001023"),
            ["requests.edit"]        = Guid.Parse("00000000-0000-0000-0000-000000001024"),
            ["requests.manage"]      = Guid.Parse("00000000-0000-0000-0000-000000001025"),
            ["requests.delete"]      = Guid.Parse("00000000-0000-0000-0000-000000001026"),
            ["deliveries.view"]      = Guid.Parse("00000000-0000-0000-0000-000000001027"),
            ["deliveries.edit"]      = Guid.Parse("00000000-0000-0000-0000-000000001028"),
            ["deliveries.create"]    = Guid.Parse("00000000-0000-0000-0000-000000001029"),
            ["visits.view"]          = Guid.Parse("00000000-0000-0000-0000-000000001030"),
            ["visits.edit"]          = Guid.Parse("00000000-0000-0000-0000-000000001031"),
            ["visits.create"]        = Guid.Parse("00000000-0000-0000-0000-000000001032"),
            ["announcements.view"]   = Guid.Parse("00000000-0000-0000-0000-000000001033"),
            ["announcements.edit"]   = Guid.Parse("00000000-0000-0000-0000-000000001034"),
            ["announcements.create"] = Guid.Parse("00000000-0000-0000-0000-000000001035"),
            ["users.edit"]           = Guid.Parse("00000000-0000-0000-0000-000000001036"),
        };

        public static Guid UserIdFor(string permission) =>
            _userIdByPermission.TryGetValue(permission, out var id)
                ? id
                : throw new ArgumentException($"No seeded user for permission '{permission}'.", nameof(permission));

        // Tenant-scope counterpart of the map above: per permission a user whose ONLY grant is an Active
        // membership (in TenantSlug) on a Tenant-scope role carrying that permission. Mint a token via
        // GenerateToken(permission)/CreateUserToken(id), send X-Tenant: TenantSlug, and hit the endpoint —
        // the permission resolves only through the tenant branch. Must match
        // IntegrationTestSeedData.TenantFixtures (UserId = 00000000-0000-0000-0000-0000000011NN).
        private static readonly Dictionary<string, Guid> _tenantUserIdByPermission = new()
        {
            ["users.manage"]         = Guid.Parse("00000000-0000-0000-0000-000000001101"),
            ["roles.manage"]         = Guid.Parse("00000000-0000-0000-0000-000000001102"),
            ["settings.view"]        = Guid.Parse("00000000-0000-0000-0000-000000001109"),
            ["tenants.create"]       = Guid.Parse("00000000-0000-0000-0000-000000001110"),
            ["tenants.view"]         = Guid.Parse("00000000-0000-0000-0000-000000001111"),
            ["tenants.edit"]         = Guid.Parse("00000000-0000-0000-0000-000000001112"),
            ["memberships.manage"]   = Guid.Parse("00000000-0000-0000-0000-000000001113"),
            ["apartments.create"]    = Guid.Parse("00000000-0000-0000-0000-000000001114"),
            ["apartments.view"]      = Guid.Parse("00000000-0000-0000-0000-000000001115"),
            ["apartments.edit"]      = Guid.Parse("00000000-0000-0000-0000-000000001116"),
            ["settings.create"]      = Guid.Parse("00000000-0000-0000-0000-000000001117"),
            ["settings.edit"]        = Guid.Parse("00000000-0000-0000-0000-000000001118"),
            ["notifications.view"]   = Guid.Parse("00000000-0000-0000-0000-000000001119"),
            ["notifications.create"] = Guid.Parse("00000000-0000-0000-0000-000000001120"),
            ["notifications.edit"]   = Guid.Parse("00000000-0000-0000-0000-000000001121"),
            ["requests.view"]        = Guid.Parse("00000000-0000-0000-0000-000000001123"),
            ["requests.edit"]        = Guid.Parse("00000000-0000-0000-0000-000000001124"),
            ["requests.manage"]      = Guid.Parse("00000000-0000-0000-0000-000000001125"),
            ["requests.delete"]      = Guid.Parse("00000000-0000-0000-0000-000000001126"),
            ["deliveries.view"]      = Guid.Parse("00000000-0000-0000-0000-000000001127"),
            ["deliveries.edit"]      = Guid.Parse("00000000-0000-0000-0000-000000001128"),
            ["deliveries.create"]    = Guid.Parse("00000000-0000-0000-0000-000000001129"),
            ["visits.view"]          = Guid.Parse("00000000-0000-0000-0000-000000001130"),
            ["visits.edit"]          = Guid.Parse("00000000-0000-0000-0000-000000001131"),
            ["visits.create"]        = Guid.Parse("00000000-0000-0000-0000-000000001132"),
            ["announcements.view"]   = Guid.Parse("00000000-0000-0000-0000-000000001133"),
            ["announcements.edit"]   = Guid.Parse("00000000-0000-0000-0000-000000001134"),
            ["announcements.create"] = Guid.Parse("00000000-0000-0000-0000-000000001135"),
            ["users.edit"]           = Guid.Parse("00000000-0000-0000-0000-000000001136"),
        };

        public static Guid TenantUserIdFor(string permission) =>
            _tenantUserIdByPermission.TryGetValue(permission, out var id)
                ? id
                : throw new ArgumentException($"No seeded tenant user for permission '{permission}'.", nameof(permission));

        // The fixed Active tenant those memberships live in. Must match
        // IntegrationTestSeedData.IntegrationTenantId / IntegrationTenantSlug (and the Tenants module seed).
        public const string TenantSlug = "integration-test";
        public static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-0000000000E2");

        // "Rol Public": a seeded user assigned to a Platform role carrying ZERO permissions. Mint a token
        // via JwtTokenFactory.GeneratePublicToken() to assert a [HasPermission(code)] endpoint returns 403
        // for a real, existing user that simply lacks the permission (distinct from an unknown-user token).
        // Must match IntegrationTestSeedData.PublicUserId (00000000-0000-0000-0000-000000001000).
        public static readonly Guid PublicUserId = Guid.Parse("00000000-0000-0000-0000-000000001000");
    }
}
