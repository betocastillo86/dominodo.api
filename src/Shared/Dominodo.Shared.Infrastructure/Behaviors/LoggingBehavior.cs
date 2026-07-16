using Dominodo.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dominodo.Shared.Infrastructure.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {Request}", name);
        var response = await next();
        if (response.IsSuccess)
        {
            logger.LogInformation("Handled {Request}", name);
        }
        else
        {
            logger.LogWarning("Request {Request} failed: {ErrorCode}", name, response.Error.Code);
        }

        return response;
    }
}
