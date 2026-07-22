using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Requests.DeleteRequest;

// Hard-deletes a request (requests.delete). The repository scopes the load to the current tenant, so a
// caller can only delete their own tenant's requests.
internal sealed record DeleteRequestCommand(Guid RequestId) : ICommand;

internal sealed class DeleteRequestCommandHandler(IRequestRepository requests)
    : ICommandHandler<DeleteRequestCommand>
{
    public async Task<Result> Handle(DeleteRequestCommand command, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        requests.Remove(request);
        return Result.Success();
    }
}
