using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;
using FluentValidation;

namespace Dominodo.Tenants.Application.Tenants.ChangeTenantStatus;

internal sealed record ChangeTenantStatusCommand(Guid TenantId, string Status) : ICommand;

internal sealed class ChangeTenantStatusCommandValidator : AbstractValidator<ChangeTenantStatusCommand>
{
    public ChangeTenantStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<TenantStatus>(status, ignoreCase: false, out _))
            .WithMessage("Status must be one of 'Onboarding', 'Active' or 'Suspended'.");
    }
}

internal sealed class ChangeTenantStatusCommandHandler(ITenantRepository tenants)
    : ICommandHandler<ChangeTenantStatusCommand>
{
    public async Task<Result> Handle(ChangeTenantStatusCommand command, CancellationToken ct)
    {
        var tenant = await tenants.GetByIdAsync(command.TenantId, ct);
        if (tenant is null)
        {
            return Error.NotFound("Tenant.NotFound", "Tenant not found.");
        }

        var status = Enum.Parse<TenantStatus>(command.Status);

        return status switch
        {
            TenantStatus.Active => tenant.Activate(),
            TenantStatus.Suspended => tenant.Suspend(),
            TenantStatus.Onboarding => tenant.ReturnToOnboarding(),
            _ => Error.Validation("Tenant.InvalidStatus", "Unknown tenant status."),
        };
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
    }
}
