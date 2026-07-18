using Dominodo.Tenants.Domain.Apartments;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.CreateApartment;

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
