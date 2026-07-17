namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/users</c>. Mirrors the API's
/// <c>RegisterUserRequest</c> by value (not by reference) — if the API contract drifts,
/// these tests break loudly. That break is the product, not a defect.
/// </summary>
public sealed record NewUserModel
{
    public string Phone { get; init; } = default!;
    public string? Email { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Password { get; init; } = default!;
}
