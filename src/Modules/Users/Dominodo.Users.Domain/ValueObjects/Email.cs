using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains('@'))
        {
            return Error.Validation("Email.Invalid", "Email is not a valid address.");
        }

        return new Email(input.Trim().ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
