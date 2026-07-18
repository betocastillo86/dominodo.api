using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;

namespace Dominodo.Tenants.Application.Tenants.CreateTenant;

internal sealed class CreateTenantCommandHandler(ITenantRepository tenants)
    : ICommandHandler<CreateTenantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTenantCommand command, CancellationToken ct)
    {
        var type = Enum.Parse<TenantType>(command.Type);

        if (await tenants.ExistsBySlugAsync(command.Slug, ct))
        {
            return Error.Conflict("Tenant.SlugAlreadyExists", "A tenant with this slug already exists.");
        }

        var tenantResult = Tenant.Create(
            command.Slug,
            command.Name,
            type,
            command.Address,
            command.City,
            command.Country,
            command.LegalId);

        if (tenantResult.IsFailure)
        {
            return tenantResult.Error;
        }

        var tenant = tenantResult.Value;

        if (command.Branding is not null)
        {
            tenant.SetBranding(command.Branding);
        }

        if (command.Settings is not null)
        {
            tenant.SetSettings(command.Settings);
        }

        tenants.Add(tenant);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return tenant.Id;
    }
}
