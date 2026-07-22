namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>PUT /api/v1/users/{id}</c>. Mirrors the API's
/// <c>UpdateUserRequest</c> by value (not by reference) — if the API contract drifts, these
/// tests break loudly. That break is the product, not a defect. <c>Email</c> is optional
/// (null clears it); the other three are required by the validator.
/// </summary>
public sealed record UpdateUserModel
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string? Email { get; init; }
    public string PreferredLanguage { get; init; } = default!;
}
