namespace Dominodo.E2E.Core.Faker;

/// <summary>
/// Bogus extensions that generate data matching the API's validation rules,
/// so the <c>Arrange</c> always produces valid input by default.
/// </summary>
public static class DominodoFakerExtensions
{
    /// <summary>
    /// A valid E.164 phone matching <c>^\+[1-9]\d{6,14}$</c>: a leading '+',
    /// a first digit 1-9, then 7-14 more digits (8-15 total digits).
    /// </summary>
    public static string E164Phone(this Bogus.Faker faker)
    {
        var firstDigit = faker.Random.Int(1, 9);
        var remainingCount = faker.Random.Int(9, 12);
        var rest = faker.Random.String2(remainingCount, "0123456789");
        return $"+{firstDigit}{rest}";
    }

    /// <summary>
    /// A password satisfying 8-128 chars with at least one upper, one lower, and one digit.
    /// </summary>
    public static string StrongPassword(this Bogus.Faker faker)
    {
        var upper = faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var lower = faker.Random.String2(5, "abcdefghijklmnopqrstuvwxyz");
        var digits = faker.Random.String2(3, "0123456789");
        return $"{upper}{lower}{digits}";
    }
}
