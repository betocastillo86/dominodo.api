using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;
using FluentValidation;

namespace Dominodo.Tenants.Application.Tenants.CreateTenant;

internal sealed record CreateTenantCommand(
    string Slug,
    string Name,
    TenantType Type,
    string Address,
    string City,
    string Country,
    string? LegalId,
    string? Branding,
    string? Settings) : ICommand<Guid>;

internal sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must be lowercase kebab-case (letters, digits and hyphens only).");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalId).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Country).MaximumLength(100);

        RuleFor(x => x.Type).IsInEnum();
    }
}

internal sealed class CreateTenantCommandHandler(ITenantRepository tenants)
    : ICommandHandler<CreateTenantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTenantCommand command, CancellationToken ct)
    {
        if (await tenants.ExistsBySlugAsync(command.Slug, ct))
        {
            return Error.Conflict("Tenant.SlugAlreadyExists", "A tenant with this slug already exists.");
        }

        var tenantResult = Tenant.Create(
            command.Slug,
            command.Name,
            command.Type,
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
