namespace Dominodo.E2E.Clients.Core.Handlers;

/// <summary>
/// Passthrough extension point for cross-cutting auth policies. Today the bearer token flows
/// via the Refit <c>[Authorize("Bearer")]</c> parameter, so this handler rewrites nothing;
/// it exists so a future policy (e.g. token refresh) has a place to live.
/// </summary>
public sealed class AuthorizationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return base.SendAsync(request, cancellationToken);
    }
}
