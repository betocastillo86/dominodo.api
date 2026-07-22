using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Requests.ChangeRequestStatus;

// Drives the request lifecycle (requests.manage). The aggregate validates the transition graph, appends
// a RequestStatusHistory row and raises the matching domain event(s).
internal sealed record ChangeRequestStatusCommand(
    Guid RequestId,
    RequestStatus Status,
    string? Note) : ICommand;

internal sealed class ChangeRequestStatusCommandValidator : AbstractValidator<ChangeRequestStatusCommand>
{
    public ChangeRequestStatusCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

internal sealed class ChangeRequestStatusCommandHandler(
    IRequestRepository requests,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<ChangeRequestStatusCommand>
{
    public async Task<Result> Handle(ChangeRequestStatusCommand command, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        return request.ChangeStatus(command.Status, currentUser.UserId, clock.UtcNow, command.Note);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
    }
}
