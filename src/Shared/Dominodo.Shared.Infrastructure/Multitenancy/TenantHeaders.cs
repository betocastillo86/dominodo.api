namespace Dominodo.Shared.Infrastructure.Multitenancy;

/// <summary>
/// Well-known HTTP header names used to resolve the current tenant (a.k.a. "site").
/// Shared by <see cref="TenantResolutionMiddleware"/> and the Swagger header filter so both
/// reference one source of truth.
/// </summary>
public static class TenantHeaders
{
    /// <summary>The header carrying the tenant/site slug (e.g. <c>X-Tenant: acme</c>).</summary>
    public const string Name = "X-Tenant";
}
