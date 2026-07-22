namespace Dominodo.Operations.Contracts;

// Public synchronous read surface of the Operations module (domain-model §3.5). Other modules depend
// only on this interface from Contracts. Takes explicit ids (not the request's tenant context) — a
// sanctioned cross-module read.
public interface IOperationsModuleApi
{
    Task<int> GetOpenRequestsCountAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<RequestSummaryDto?> GetRequestSummaryAsync(Guid requestId, CancellationToken cancellationToken = default);
}
