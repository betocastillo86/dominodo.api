namespace Dominodo.Admin.Contracts;

// The resolved value of a setting (tenant override if present, otherwise global) returned by the facade.
public sealed record SystemSettingValueDto(string Value, string ValueType);
