using Dominodo.E2E.Core;
using Dominodo.E2E.Clients.Core.Context;

namespace Dominodo.E2E.Clients.Core.Handlers;

/// <summary>
/// Injects <c>X-Correlation-Id</c> and <c>X-TestName</c> from <see cref="TestExecutionContext"/>
/// so a test's requests can be traced against the API logs end to end.
/// </summary>
public sealed class CorrelationIdHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = TestExecutionContext.CorrelationId;
        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(DominodoConstants.Headers.CorrelationId))
        {
            request.Headers.Add(DominodoConstants.Headers.CorrelationId, correlationId);
        }

        var testName = TestExecutionContext.TestName;
        if (!string.IsNullOrWhiteSpace(testName) && !request.Headers.Contains(DominodoConstants.Headers.TestName))
        {
            request.Headers.Add(DominodoConstants.Headers.TestName, testName);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
