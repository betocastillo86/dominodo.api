using Dominodo.Tenants.Domain.Tenants;
using FluentValidation;

namespace Dominodo.Tenants.Application.Tenants.ChangeTenantStatus;

internal sealed class ChangeTenantStatusCommandValidator : AbstractValidator<ChangeTenantStatusCommand>
{
    public ChangeTenantStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<TenantStatus>(status, ignoreCase: false, out _))
            .WithMessage("Status must be one of 'Onboarding', 'Active' or 'Suspended'.");
    }
}
