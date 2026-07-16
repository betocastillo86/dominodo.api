using System.ComponentModel.DataAnnotations;

namespace Dominodo.Adapters.WhatsApp;

public sealed class WhatsAppSenderOptions
{
    public const string SectionName = "Adapters:WhatsApp";

    [Required]
    public required string BaseUrl { get; init; }

    public string? ApiKey { get; init; }

    public int TimeoutSeconds { get; init; } = 10;
}
