using Polly;
using Polly.Retry;

namespace Dominodo.E2E.Core.Policies;

/// <summary>
/// Assertion-retry helpers for eventual consistency: poll an action until its result
/// satisfies a predicate (or a timeout elapses). This is distinct from transport retries
/// (5xx/timeout) handled by the HTTP handler pipeline.
/// </summary>
public static class RetryPolicies
{
    /// <summary>
    /// Repeatedly invokes <paramref name="action"/> until <paramref name="predicate"/> returns
    /// true or <paramref name="timeout"/> elapses. Returns the last result. Use for assertions
    /// that depend on an integration event having been processed.
    /// </summary>
    public static async Task<T> Until<T>(
        Func<Task<T>> action,
        Func<T, bool> predicate,
        TimeSpan? timeout = null,
        TimeSpan? delay = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var effectiveDelay = delay ?? TimeSpan.FromMilliseconds(500);

        var pipeline = new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                ShouldHandle = new PredicateBuilder<T>().HandleResult(r => !predicate(r)),
                Delay = effectiveDelay,
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Constant,
            })
            .AddTimeout(effectiveTimeout)
            .Build();

        return await pipeline.ExecuteAsync(async _ => await action());
    }
}
