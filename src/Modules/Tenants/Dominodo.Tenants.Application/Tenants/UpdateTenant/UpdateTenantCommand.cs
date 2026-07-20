using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Ports;
using FluentValidation;

namespace Dominodo.Tenants.Application.Tenants.UpdateTenant;

// Slug is immutable once set (domain-model §2.1, plan Phase 2) and is therefore not part of the update.
internal sealed record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string? LegalId,
    string Address,
    string City,
    string Country) : ICommand;

internal sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalId).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Country).MaximumLength(100);
    }
}

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
