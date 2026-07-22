using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Admin.Application.Configuration.CreateSystemSetting;

internal sealed record CreateSystemSettingCommand(
    string Key,
    string Value,
    SystemSettingValueType ValueType) : ICommand<string>;

internal sealed class CreateSystemSettingCommandValidator : AbstractValidator<CreateSystemSettingCommand>
{
    public CreateSystemSettingCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotNull();
        RuleFor(x => x.ValueType).IsInEnum();
    }
}

// Writes set TenantId from ITenantContext when a tenant is resolved (an override); with no X-Tenant the
// write targets the global row (TenantId = null). Combined with [HasPermission(settings.create)] this
// reproduces "global config only by a Platform role" purely through permission resolution — see doc 12.
internal sealed class CreateSystemSettingCommandHandler(
    ISystemSettingRepository settings,
    ITenantContext tenant,
    IClock clock)
    : ICommandHandler<CreateSystemSettingCommand, string>
{
    public async Task<Result<string>> Handle(CreateSystemSettingCommand command, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var key = command.Key.Trim();

        if (await settings.ExistsAsync(key, tenantId, ct))
        {
            return Error.Conflict(
                "SystemSetting.AlreadyExists",
                "A setting with this key already exists in this scope.");
        }

        var result = SystemSetting.Create(key, tenantId, command.Value, command.ValueType, clock);
        if (result.IsFailure)
        {
            return result.Error;
        }

        settings.Add(result.Value);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return result.Value.Key;
    }
}
