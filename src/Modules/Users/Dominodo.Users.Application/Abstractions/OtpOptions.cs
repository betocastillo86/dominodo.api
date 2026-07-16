namespace Dominodo.Users.Application.Abstractions;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int Length { get; init; } = 6;
    public int TtlMinutes { get; init; } = 10;
    public int MaxAttempts { get; init; } = 5;
}
