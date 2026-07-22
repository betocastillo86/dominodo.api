using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;
using FluentValidation;

namespace Dominodo.Operations.Application.Requests.AddRequestParticipant;

// Adds a follower to a request (requests.edit). The user must exist in Users (facade read).
internal sealed record AddRequestParticipantCommand(Guid RequestId, Guid UserId) : ICommand<Guid>;

internal sealed class AddRequestParticipantCommandValidator : AbstractValidator<AddRequestParticipantCommand>
{
    public AddRequestParticipantCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class AddRequestParticipantCommandHandler(
    IRequestRepository requests,
    IUsersModuleApi usersModule,
    IClock clock)
    : ICommandHandler<AddRequestParticipantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddRequestParticipantCommand command, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return Error.NotFound("Request.NotFound", "Request not found.");
        }

        var user = await usersModule.GetUserByIdAsync(command.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", "The referenced user does not exist.");
        }

        var result = request.AddParticipant(
            command.UserId, ParticipantType.Follower, ParticipantSource.Admin, clock.UtcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        return result.Value.Id;
    }
}
