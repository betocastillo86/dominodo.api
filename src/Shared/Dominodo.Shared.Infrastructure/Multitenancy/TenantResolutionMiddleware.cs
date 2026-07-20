using Dominodo.Shared.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Shared.Infrastructure.Multitenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string TenantIdKey = "TenantId";
    private const string TenantHeader = TenantHeaders.Name;

    public async Task Invoke(HttpContext ctx, ITenantDirectory directory)
    {
        var slug = ctx.Request.Headers[TenantHeader].ToString();

        if (!string.IsNullOrWhiteSpace(slug))
        {
            var tenantId = await directory.ResolveSlugAsync(slug, ctx.RequestAborted);
            if (tenantId is null)
            {
                await RejectAsync(ctx, 400, "Tenant.Unknown", "The specified tenant does not exist.");
                return;
            }

            ctx.Items[TenantIdKey] = tenantId.Value;
        }

        // Tenant access is enforced downstream by permission resolution, not here: [HasPermission] +
        // CachingPermissionProvider's tenant branch resolve platform ∪ the caller's Active-membership
        // permissions for the resolved (user, tenant), so a caller with no Active membership (and thus
        // no tenant permission) fails closed at authorization. Cross-tenant authority is a permission
        // (SuperAdmin's platform grant), never a hardcoded role — no IsInRole here. A blanket
        // "must hold SOME membership" gate is intentionally omitted (per-endpoint permissions suffice).

        await next(ctx);
    }

    private static async Task RejectAsync(HttpContext ctx, int status, string title, string detail)
    {
        var problem = new ProblemDetails { Title = title, Detail = detail, Status = status };
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
