namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated response for <c>POST /api/v1/auth/login</c> and <c>/auth/refresh</c>.
/// Mirrors the API's <c>AuthTokensResponse</c> by value.
/// </summary>
public sealed record AuthTokensModel
{
    public string AccessToken { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTimeOffset ExpiresAt { get; init; }
}
