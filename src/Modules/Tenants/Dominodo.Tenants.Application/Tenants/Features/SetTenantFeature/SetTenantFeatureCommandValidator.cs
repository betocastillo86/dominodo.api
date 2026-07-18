using Dominodo.Tenants.Domain.Tenants;
using FluentValidation;

namespace Dominodo.Tenants.Application.Tenants.Features.SetTenantFeature;

internal sealed class SetTenantFeatureCommandValidator : AbstractValidator<SetTenantFeatureCommand>
{
    public SetTenantFeatureCommandValidator()
    {
        RuleFor(x => x.FeatureKey)
            .Must(key => Enum.TryParse<FeatureKey>(key, ignoreCase: false, out _))
            .WithMessage("FeatureKey must be one of 'Requests', 'Deliveries', 'Visits', 'Announcements' or 'WhatsApp'.");
    }
}
