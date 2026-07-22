using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.ChangeApartmentStatus;

internal sealed record ChangeApartmentStatusCommand(Guid ApartmentId, ApartmentStatus Status) : ICommand;

internal sealed class ChangeApartmentStatusCommandValidator : AbstractValidator<ChangeApartmentStatusCommand>
{
    public ChangeApartmentStatusCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

internal sealed class ChangeApartmentStatusCommandHandler(IApartmentRepository apartments)
    : ICommandHandler<ChangeApartmentStatusCommand>
{
    public async Task<Result> Handle(ChangeApartmentStatusCommand command, CancellationToken ct)
    {
        var apartment = await apartments.GetByIdAsync(command.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        return command.Status switch
        {
            ApartmentStatus.Occupied => apartment.MarkOccupied(),
            ApartmentStatus.Vacant => apartment.MarkVacant(),
            _ => Error.Validation("Apartment.InvalidStatus", "Unknown apartment status."),
        };
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
    }
}
