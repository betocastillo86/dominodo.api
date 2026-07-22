using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Visits.FinishVisit;

// InProgress → Finished (visits.edit). Sets ExitAtUtc and an optional amount paid (e.g. visitor parking).
internal sealed record FinishVisitCommand(Guid VisitId, decimal? AmountPaid) : ICommand;

internal sealed class FinishVisitCommandHandler(IVisitRepository visits, IClock clock)
    : ICommandHandler<FinishVisitCommand>
{
    public async Task<Result> Handle(FinishVisitCommand command, CancellationToken ct)
    {
        var visit = await visits.GetByIdAsync(command.VisitId, ct);
        if (visit is null)
        {
            return Error.NotFound("Visit.NotFound", "Visit not found.");
        }

        return visit.Finish(clock.UtcNow, command.AmountPaid);
    }
}
