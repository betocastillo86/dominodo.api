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

        // TODO (Fase 4 — Membership slice, doc 12): enforce that an authenticated caller may act on
        // the resolved tenant only if they have a Membership in it. This replaces the old, inert
        // `tenant_id`-claim reconciliation (that claim was never emitted and assumed one tenant per
        // token). Cross-tenant authority must be a permission, not a hardcoded role — no IsInRole here.

        await next(ctx);
    }

    private static async Task RejectAsync(HttpContext ctx, int status, string title, string detail)
    {
        var problem = new ProblemDetails { Title = title, Detail = detail, Status = status };
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
