using System.Net;

namespace Dominodo.E2E.Clients.Core.Handlers;

/// <summary>
/// Transport-level retries only: transient 5xx and request timeouts. This is NOT an
/// assertion retry — eventual-consistency polling lives in <c>RetryPolicies.Until</c>.
/// A short, fixed-count retry smooths over the API still warming up or a flaky socket.
/// </summary>
public sealed class DefaultRetryHandler : DelegatingHandler
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                if (!IsTransient(response.StatusCode) || attempt == MaxAttempts)
                {
                    return response;
                }
            }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            {
                // transient transport failure; retry
            }
            catch (TaskCanceledException) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                // request timeout; retry
            }

            response?.Dispose();
            await Task.Delay(Delay, cancellationToken);
        }

        return response!;
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 500;
    }
}
