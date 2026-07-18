namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/auth/login</c>. Mirrors the API's
/// <c>LoginRequest</c> by value — drift breaks loudly, and that break is the product.
/// </summary>
public sealed record LoginModel
{
    public string Phone { get; init; } = default!;
    public string Password { get; init; } = default!;
}
