namespace Dominodo.E2E.Clients.Core.Api;

/// <summary>
/// HTTP client settings bound from the <c>ApiSettings</c> configuration section
/// (section name kept identical to Pollaya for consistency).
/// </summary>
public sealed class ApiSettings
{
    public const string SectionName = "ApiSettings";

    public string BaseUrl { get; init; } = default!;
    public string? DefaultTenantSlug { get; init; }
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>When true, <c>LoggingHandler</c> logs only non-success responses.</summary>
    public bool OnlyLogErrors { get; init; }
}
