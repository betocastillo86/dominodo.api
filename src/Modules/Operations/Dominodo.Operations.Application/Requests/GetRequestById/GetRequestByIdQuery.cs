using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Requests.GetRequestById;

internal sealed record GetRequestByIdQuery(Guid RequestId) : IQuery<RequestDetailDto>;

internal sealed class GetRequestByIdQueryHandler(
    IRequestRepository requests,
    IResourceAccessAuthorizer authorizer)
    : IQueryHandler<GetRequestByIdQuery, RequestDetailDto>
{
    public async Task<Result<RequestDetailDto>> Handle(GetRequestByIdQuery query, CancellationToken ct)
    {
        // Scoped by the repository — never returns another tenant's request.
        var request = await requests.GetByIdAsync(query.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        // Dual-mode: staff hold requests.view (any request), or the caller is a participant (reporter/
        // follower) of THIS request. Denial returns the same NotFound as a missing row — leak-safe.
        var allowed = await authorizer.HasAccessAsync(
            Permissions.RequestsView,
            userId => request.IsParticipant(userId),
            ct);
        if (!allowed)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        return RequestMappers.ToDetailDto(request);
    }
}
