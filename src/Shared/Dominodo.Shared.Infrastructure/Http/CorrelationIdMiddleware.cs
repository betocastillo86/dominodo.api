using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Dominodo.Shared.Infrastructure.Http;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task Invoke(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        ctx.Response.Headers[HeaderName] = correlationId;
        ctx.Items[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(ctx);
        }
    }
}
