using Dominodo.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Dominodo.Shared.Infrastructure.Multitenancy;

internal sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private const string TenantIdKey = "TenantId";

    public Guid TenantId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No HTTP context available.");

            if (ctx.Items.TryGetValue(TenantIdKey, out var raw) && raw is Guid id)
            {
                return id;
            }

            throw new InvalidOperationException("No tenant has been resolved for this request.");
        }
    }

    public bool HasTenant
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            return ctx is not null && ctx.Items.ContainsKey(TenantIdKey);
        }
    }

    public bool IsSuperAdmin =>
        httpContextAccessor.HttpContext?.User.IsInRole("SuperAdmin") ?? false;
}
