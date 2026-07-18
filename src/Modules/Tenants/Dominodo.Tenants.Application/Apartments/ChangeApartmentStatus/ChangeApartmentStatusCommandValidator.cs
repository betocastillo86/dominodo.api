using Dominodo.Tenants.Domain.Apartments;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.ChangeApartmentStatus;

internal sealed class ChangeApartmentStatusCommandValidator : AbstractValidator<ChangeApartmentStatusCommand>
{
    public ChangeApartmentStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<ApartmentStatus>(status, ignoreCase: false, out _))
            .WithMessage("Status must be either 'Occupied' or 'Vacant'.");
    }
}
