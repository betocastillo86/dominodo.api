using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.CreateApartment;

internal sealed record CreateApartmentCommand(
    string Number,
    string Type,
    string? Tower,
    string? Attributes) : ICommand<Guid>;

internal sealed class CreateApartmentCommandValidator : AbstractValidator<CreateApartmentCommand>
{
    public CreateApartmentCommandValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Tower).MaximumLength(50);

        RuleFor(x => x.Type)
            .Must(type => Enum.TryParse<ApartmentType>(type, ignoreCase: false, out _))
            .WithMessage("Type must be one of 'Apartment', 'House', 'Commercial', 'Parking' or 'Storage'.");
    }
}

// Writes set TenantId from ITenantContext (doc 09). The repository already scopes the conflict check to
// the current tenant, so a duplicate (Tower, Number) is detected within this tenant only.
internal sealed class CreateApartmentCommandHandler(
    IApartmentRepository apartments,
    ITenantContext tenant)
    : ICommandHandler<CreateApartmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateApartmentCommand command, CancellationToken ct)
    {
        var type = Enum.Parse<ApartmentType>(command.Type);
        var tower = string.IsNullOrWhiteSpace(command.Tower) ? null : command.Tower.Trim();
        var number = command.Number.Trim();

        if (await apartments.ExistsByTowerAndNumberAsync(tower, number, ct))
        {
            return Error.Conflict(
                "Apartment.AlreadyExists",
                "An apartment with this tower and number already exists in this tenant.");
        }

        var apartmentResult = Apartment.Create(tenant.TenantId, number, type, tower);
        if (apartmentResult.IsFailure)
        {
            return apartmentResult.Error;
        }

        var apartment = apartmentResult.Value;

        if (command.Attributes is not null)
        {
            apartment.SetAttributes(command.Attributes);
        }

        apartments.Add(apartment);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return apartment.Id;
    }
}
