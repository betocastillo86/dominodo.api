using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;
using FluentValidation;

namespace Dominodo.Tenants.Application.Tenants.Features.SetTenantFeature;

internal sealed record SetTenantFeatureCommand(Guid TenantId, FeatureKey FeatureKey, bool Enabled) : ICommand;

internal sealed class SetTenantFeatureCommandValidator : AbstractValidator<SetTenantFeatureCommand>
{
    public SetTenantFeatureCommandValidator()
    {
        RuleFor(x => x.FeatureKey).IsInEnum();
    }
}

// Platform-scoped: SuperAdmin manages feature enablement per conjunto (no X-Tenant). Idempotent upsert.
internal sealed class SetTenantFeatureCommandHandler(ITenantRepository tenants)
    : ICommandHandler<SetTenantFeatureCommand>
{
    public async Task<Result> Handle(SetTenantFeatureCommand command, CancellationToken ct)
    {
        var tenant = await tenants.GetByIdWithFeaturesAsync(command.TenantId, ct);
        if (tenant is null)
        {
            return Error.NotFound("Tenant.NotFound", "Tenant not found.");
        }

        tenant.SetFeature(command.FeatureKey, command.Enabled);

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
