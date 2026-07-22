using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Requests.AddRequestUpdate;

// Adds a timeline entry. Dual-mode: staff with requests.edit, OR a participant of this request (a
// resident following their own PQRS can comment / add evidence). The current user is the author.
internal sealed record AddRequestUpdateCommand(
    Guid RequestId,
    RequestUpdateType Type,
    string? Body,
    bool IsInternal) : ICommand<Guid>;

internal sealed class AddRequestUpdateCommandValidator : AbstractValidator<AddRequestUpdateCommand>
{
    public AddRequestUpdateCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Body).MaximumLength(4000);
    }
}

internal sealed class AddRequestUpdateCommandHandler(
    IRequestRepository requests,
    IResourceAccessAuthorizer authorizer,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddRequestUpdateCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddRequestUpdateCommand command, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        var allowed = await authorizer.HasAccessAsync(
            Permissions.RequestsEdit,
            userId => request.IsParticipant(userId),
            ct);
        if (!allowed)
        {
            return Error.Forbidden("Request.Forbidden", "You cannot add updates to this request.");
        }

        var result = request.AddUpdate(
            currentUser.UserId, command.Type, command.Body, command.IsInternal, clock.UtcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        return result.Value.Id;
    }
}
