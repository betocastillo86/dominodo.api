using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Visits.UpdateVisit;

// Edits visit details (visits.edit). Finishing goes through FinishVisit.
internal sealed record UpdateVisitCommand(
    Guid VisitId,
    VisitType Type,
    string VisitorName,
    string? VisitorDocument,
    string? PhotoUrl,
    string? VehiclePlate,
    string? Metadata) : ICommand;

internal sealed class UpdateVisitCommandValidator : AbstractValidator<UpdateVisitCommand>
{
    public UpdateVisitCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.VisitorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.VisitorDocument).MaximumLength(50);
        RuleFor(x => x.PhotoUrl).MaximumLength(2000);
        RuleFor(x => x.VehiclePlate).MaximumLength(20);
    }
}

internal sealed class UpdateVisitCommandHandler(IVisitRepository visits)
    : ICommandHandler<UpdateVisitCommand>
{
    public async Task<Result> Handle(UpdateVisitCommand command, CancellationToken ct)
    {
        var visit = await visits.GetByIdAsync(command.VisitId, ct);
        if (visit is null)
        {
            return Error.NotFound("Visit.NotFound", "Visit not found.");
        }

        var result = visit.UpdateDetails(
            command.Type, command.VisitorName, command.VisitorDocument, command.PhotoUrl, command.VehiclePlate);
        if (result.IsFailure)
        {
            return result.Error;
        }

        visit.SetMetadata(command.Metadata);
        return Result.Success();
    }
}
