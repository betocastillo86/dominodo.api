using Dominodo.Shared.Kernel;

namespace Dominodo.Shared.Infrastructure.Multitenancy;

public static class TenantQueryExtensions
{
    public static IQueryable<T> ForCurrentTenant<T>(this IQueryable<T> query, ITenantContext tenant)
        where T : class, ITenantOwned
        => query.Where(e => e.TenantId == tenant.TenantId);
}
