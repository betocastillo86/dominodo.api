using Dominodo.Tenants.Domain.Apartments;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.Residents.AssignResident;

internal sealed class AssignResidentCommandValidator : AbstractValidator<AssignResidentCommand>
{
    public AssignResidentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.RelationType)
            .Must(rt => Enum.TryParse<ResidentRelationType>(rt, ignoreCase: false, out _))
            .WithMessage("RelationType must be either 'Owner' or 'Renter'.");
    }
}
