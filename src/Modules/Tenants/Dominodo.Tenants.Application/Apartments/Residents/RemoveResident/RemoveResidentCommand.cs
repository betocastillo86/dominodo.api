using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.Residents.RemoveResident;

internal sealed record RemoveResidentCommand(Guid ApartmentId, Guid ResidentId) : ICommand;

internal sealed class RemoveResidentCommandHandler(IApartmentRepository apartments)
    : ICommandHandler<RemoveResidentCommand>
{
    public async Task<Result> Handle(RemoveResidentCommand command, CancellationToken ct)
    {
        var apartment = await apartments.GetByIdWithResidentsAsync(command.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return apartment.RemoveResident(command.ResidentId);
    }
}
