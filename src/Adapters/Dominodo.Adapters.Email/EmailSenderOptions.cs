using System.ComponentModel.DataAnnotations;

namespace Dominodo.Adapters.Email;

public sealed class EmailSenderOptions
{
    public const string SectionName = "Adapters:Email";

    [Required]
    public required string BaseUrl { get; init; }

    public string? ApiKey { get; init; }

    public int TimeoutSeconds { get; init; } = 10;
}
