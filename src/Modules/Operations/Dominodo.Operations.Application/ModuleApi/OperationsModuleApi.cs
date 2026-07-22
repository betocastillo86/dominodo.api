using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;

namespace Dominodo.Operations.Application.ModuleApi;

// Internal implementation of the public Operations facade (domain-model §3.5). Delegates straight to the
// domain ports (no MediatR round-trip for reads); takes explicit ids so it is not bound to the request's
// tenant context.
internal sealed class OperationsModuleApi(IRequestRepository requests) : IOperationsModuleApi
{
    public Task<int> GetOpenRequestsCountAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        requests.CountOpenForTenantAsync(tenantId, cancellationToken);

    public async Task<RequestSummaryDto?> GetRequestSummaryAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdForSummaryAsync(requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        return new RequestSummaryDto(
            request.Id,
            request.TenantId,
            request.Code,
            request.Type.ToString(),
            request.Status.ToString(),
            request.Priority.ToString(),
            request.Title,
            request.CreatedByUserId,
            request.AssignedToUserId);
    }
}
