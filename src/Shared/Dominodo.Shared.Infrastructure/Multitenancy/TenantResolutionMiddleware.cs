using Dominodo.Shared.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dominodo.Shared.Infrastructure.Multitenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string TenantIdKey = "TenantId";
    private const string TenantHeader = TenantHeaders.Name;

    public async Task Invoke(HttpContext ctx, ITenantDirectory directory)
    {
        var slug = ctx.Request.Headers[TenantHeader].ToString();
        var isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false;
        var isTenantUser = isAuthenticated && !ctx.User.IsInRole("SuperAdmin");

        if (!string.IsNullOrWhiteSpace(slug))
        {
            var tenantId = await directory.ResolveSlugAsync(slug, ctx.RequestAborted);
            if (tenantId is null)
            {
                await RejectAsync(ctx, 400, "Tenant.Unknown", "The specified tenant does not exist.");
                return;
            }

            ctx.Items[TenantIdKey] = tenantId.Value;

            if (isTenantUser &&
                Guid.TryParse(ctx.User.FindFirstValue("tenant_id"), out var claimTenantId) &&
                claimTenantId != tenantId.Value)
            {
                await RejectAsync(ctx, 403, "Tenant.Mismatch", "Token tenant does not match the requested tenant.");
                return;
            }
        }
        else if (isTenantUser && ctx.User.FindFirstValue("tenant_id") is not null)
        {
            await RejectAsync(ctx, 403, "Tenant.Mismatch", "Tenant header is required for authenticated users.");
            return;
        }

        await next(ctx);
    }

    private static async Task RejectAsync(HttpContext ctx, int status, string title, string detail)
    {
        var problem = new ProblemDetails { Title = title, Detail = detail, Status = status };
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
