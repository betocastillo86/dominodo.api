using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.UpdateApartment;

internal sealed record UpdateApartmentCommand(
    Guid ApartmentId,
    string Number,
    string Type,
    string? Tower,
    string? Attributes) : ICommand;

internal sealed class UpdateApartmentCommandValidator : AbstractValidator<UpdateApartmentCommand>
{
    public UpdateApartmentCommandValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Tower).MaximumLength(50);

        RuleFor(x => x.Type)
            .Must(type => Enum.TryParse<ApartmentType>(type, ignoreCase: false, out _))
            .WithMessage("Type must be one of 'Apartment', 'House', 'Commercial', 'Parking' or 'Storage'.");
    }
}

internal sealed class UpdateApartmentCommandHandler(IApartmentRepository apartments)
    : ICommandHandler<UpdateApartmentCommand>
{
    public async Task<Result> Handle(UpdateApartmentCommand command, CancellationToken ct)
    {
        // Scoped by the repository — a caller can only load their own tenant's apartment.
        var apartment = await apartments.GetByIdAsync(command.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        var type = Enum.Parse<ApartmentType>(command.Type);
        var tower = string.IsNullOrWhiteSpace(command.Tower) ? null : command.Tower.Trim();
        var number = command.Number.Trim();

        // Only re-check the unique (Tower, Number) when it actually changed — otherwise the check would
        // match the apartment itself.
        var identityChanged = apartment.Tower != tower || apartment.Number != number;
        if (identityChanged && await apartments.ExistsByTowerAndNumberAsync(tower, number, ct))
        {
            return Error.Conflict(
                "Apartment.AlreadyExists",
                "An apartment with this tower and number already exists in this tenant.");
        }

        var renameResult = apartment.Rename(number, tower);
        if (renameResult.IsFailure)
        {
            return renameResult.Error;
        }

        var typeResult = apartment.ChangeType(type);
        if (typeResult.IsFailure)
        {
            return typeResult.Error;
        }

        apartment.SetAttributes(command.Attributes);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
