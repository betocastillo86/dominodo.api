namespace Dominodo.Admin.Contracts;

// Public representation of a configuration row (domain-model §4.4). TenantId null = global value.
public sealed record SystemSettingDto(
    string Key,
    Guid? TenantId,
    string Value,
    string ValueType,
    DateTimeOffset UpdatedAtUtc);
