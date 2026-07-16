using System.Text.RegularExpressions;
using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.ValueObjects;

public sealed partial class PhoneNumber : ValueObject
{
    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation("Phone.Required", "Phone number is required.");
        }

        var trimmed = input.Trim();
        if (!E164Regex().IsMatch(trimmed))
        {
            return Error.Validation("Phone.Invalid", "Phone number must be in E.164 format (e.g. +573001234567).");
        }

        return new PhoneNumber(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;

    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex E164Regex();
}
