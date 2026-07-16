using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dominodo.Shared.Infrastructure.Http;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken token)
    {
        logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The request could not be completed. Please try again later."
        };

        ctx.Response.StatusCode = problem.Status!.Value;
        await ctx.Response.WriteAsJsonAsync(problem, token);
        return true;
    }
}
