using Dominodo.E2E.Core;
using Dominodo.E2E.Core.Context;

namespace Dominodo.E2E.Clients.Core.Handlers;

/// <summary>
/// Injects the <c>X-Tenant</c> header from <see cref="AmbientTenantContext"/> — but only when a
/// slug is set. While multitenancy is deferred, no slug means no header, so anonymous endpoints
/// (registration, get-by-id) reach the API without tripping the NullTenantDirectory.
/// </summary>
public sealed class TenantHeaderHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var slug = AmbientTenantContext.CurrentSlug;
        if (!string.IsNullOrWhiteSpace(slug) && !request.Headers.Contains(DominodoConstants.Headers.Tenant))
        {
            request.Headers.Add(DominodoConstants.Headers.Tenant, slug);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
