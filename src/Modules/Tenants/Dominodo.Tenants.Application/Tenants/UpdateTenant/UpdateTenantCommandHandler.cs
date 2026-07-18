using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Tenants.UpdateTenant;

internal sealed class UpdateTenantCommandHandler(ITenantRepository tenants)
    : ICommandHandler<UpdateTenantCommand>
{
    public async Task<Result> Handle(UpdateTenantCommand command, CancellationToken ct)
    {
        var tenant = await tenants.GetByIdAsync(command.TenantId, ct);
        if (tenant is null)
        {
            return Error.NotFound("Tenant.NotFound", "Tenant not found.");
        }

        var renameResult = tenant.Rename(command.Name);
        if (renameResult.IsFailure)
        {
            return renameResult.Error;
        }

        var profileResult = tenant.UpdateProfile(command.LegalId, command.Address, command.City, command.Country);
        if (profileResult.IsFailure)
        {
            return profileResult.Error;
        }

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
