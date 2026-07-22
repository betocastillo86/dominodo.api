using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Requests.UpdateRequest;

// Edits request fields (requests.edit). Lifecycle status is changed via ChangeRequestStatus, not here.
internal sealed record UpdateRequestCommand(
    Guid RequestId,
    RequestType Type,
    string Title,
    string Description,
    RequestPriority Priority,
    string? Category,
    string? Location,
    string? Metadata) : ICommand;

internal sealed class UpdateRequestCommandValidator : AbstractValidator<UpdateRequestCommand>
{
    public UpdateRequestCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}

internal sealed class UpdateRequestCommandHandler(IRequestRepository requests)
    : ICommandHandler<UpdateRequestCommand>
{
    public async Task<Result> Handle(UpdateRequestCommand command, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        var result = request.Update(
            command.Title,
            command.Description,
            command.Type,
            command.Priority,
            command.Category,
            command.Location);
        if (result.IsFailure)
        {
            return result.Error;
        }

        request.SetMetadata(command.Metadata);
        return Result.Success();
    }
}
