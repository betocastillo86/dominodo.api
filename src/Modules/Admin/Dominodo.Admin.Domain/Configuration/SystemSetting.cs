using Dominodo.Admin.Domain.Configuration.Events;
using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Configuration;

// Operational key/value configuration with per-tenant override (domain-model §4.4). TenantId null = the
// global value; set = a conjunto-scoped override. Value is stored as a JSON string, interpreted by
// ValueType. Uniqueness is enforced on (Key, TenantId) at the persistence layer.
public sealed class SystemSetting : AggregateRoot
{
    private SystemSetting() { } // EF Core

    private SystemSetting(
        Guid id,
        string key,
        Guid? tenantId,
        string value,
        SystemSettingValueType valueType,
        DateTimeOffset updatedAtUtc) : base(id)
    {
        Key = key;
        TenantId = tenantId;
        Value = value;
        ValueType = valueType;
        UpdatedAtUtc = updatedAtUtc;
    }

    public string Key { get; private set; } = null!;
    public Guid? TenantId { get; private set; }
    public string Value { get; private set; } = null!;
    public SystemSettingValueType ValueType { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<SystemSetting> Create(
        string key,
        Guid? tenantId,
        string value,
        SystemSettingValueType valueType,
        IClock clock)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Error.Validation("SystemSetting.KeyRequired", "A setting key is required.");
        }

        if (value is null)
        {
            return Error.Validation("SystemSetting.ValueRequired", "A setting value is required.");
        }

        var setting = new SystemSetting(Guid.NewGuid(), key.Trim(), tenantId, value, valueType, clock.UtcNow);
        setting.Raise(new SystemSettingChangedDomainEvent(setting.Key, setting.TenantId));
        return setting;
    }

    public Result UpdateValue(string value, SystemSettingValueType valueType, IClock clock)
    {
        if (value is null)
        {
            return Error.Validation("SystemSetting.ValueRequired", "A setting value is required.");
        }

        Value = value;
        ValueType = valueType;
        UpdatedAtUtc = clock.UtcNow;
        Raise(new SystemSettingChangedDomainEvent(Key, TenantId));
        return Result.Success();
    }
}
