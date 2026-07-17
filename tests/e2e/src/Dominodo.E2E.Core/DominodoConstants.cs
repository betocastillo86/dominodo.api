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

    public static class Defaults
    {
        // Present but unused while multitenancy is deferred (NullTenantDirectory).
        public const string TenantSlug = "e2e-default";
    }
}
