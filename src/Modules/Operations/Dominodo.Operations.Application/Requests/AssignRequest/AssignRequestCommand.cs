using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;
using FluentValidation;

namespace Dominodo.Operations.Application.Requests.AssignRequest;

// Assigns a responsible collaborator (requests.manage). The assignee must exist in Users (cross-module
// read via the facade — NO FK across modules).
internal sealed record AssignRequestCommand(Guid RequestId, Guid AssignedToUserId) : ICommand;

internal sealed class AssignRequestCommandValidator : AbstractValidator<AssignRequestCommand>
{
    public AssignRequestCommandValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}

internal sealed class AssignRequestCommandHandler(
    IRequestRepository requests,
    IUsersModuleApi usersModule)
    : ICommandHandler<AssignRequestCommand>
{
    public async Task<Result> Handle(AssignRequestCommand command, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        var user = await usersModule.GetUserByIdAsync(command.AssignedToUserId, ct);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", "The referenced user does not exist.");
        }

        return request.Assign(command.AssignedToUserId);
    }
}
