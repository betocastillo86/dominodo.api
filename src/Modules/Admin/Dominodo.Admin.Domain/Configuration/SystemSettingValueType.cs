namespace Dominodo.Admin.Domain.Configuration;

// Declares how the JSON-stored Value should be parsed/validated (domain-model §4.4).
public enum SystemSettingValueType
{
    String,
    Int,
    Bool,
    Json
}
