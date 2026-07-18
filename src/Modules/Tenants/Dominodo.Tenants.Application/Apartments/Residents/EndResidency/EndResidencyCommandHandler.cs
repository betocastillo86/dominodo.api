using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.Residents.EndResidency;

internal sealed class EndResidencyCommandHandler(IApartmentRepository apartments)
    : ICommandHandler<EndResidencyCommand>
{
    public async Task<Result> Handle(EndResidencyCommand command, CancellationToken ct)
    {
        var apartment = await apartments.GetByIdWithResidentsAsync(command.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return apartment.EndResidency(command.ResidentId, command.EndDate);
    }
}
