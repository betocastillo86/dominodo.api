using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Admin.Application.Configuration.UpdateSystemSetting;

internal sealed record UpdateSystemSettingCommand(
    string Key,
    string Value,
    string ValueType) : ICommand;

internal sealed class UpdateSystemSettingCommandValidator : AbstractValidator<UpdateSystemSettingCommand>
{
    public UpdateSystemSettingCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotNull();

        RuleFor(x => x.ValueType)
            .Must(vt => Enum.TryParse<SystemSettingValueType>(vt, ignoreCase: false, out _))
            .WithMessage("ValueType must be one of 'String', 'Int', 'Bool' or 'Json'.");
    }
}

// Targets the exact (Key, TenantId) row for the current scope: an X-Tenant resolves the tenant override,
// its absence the global row (see the note on CreateSystemSettingCommand and doc 12).
internal sealed class UpdateSystemSettingCommandHandler(
    ISystemSettingRepository settings,
    ITenantContext tenant,
    IClock clock)
    : ICommandHandler<UpdateSystemSettingCommand>
{
    public async Task<Result> Handle(UpdateSystemSettingCommand command, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var key = command.Key.Trim();

        var setting = await settings.GetByKeyAsync(key, tenantId, ct);
        if (setting is null)
        {
            return Error.NotFound("SystemSetting.NotFound", $"No setting found for key '{key}' in this scope.");
        }

        var valueType = Enum.Parse<SystemSettingValueType>(command.ValueType);

        var result = setting.UpdateValue(command.Value, valueType, clock);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return result;
    }
}
