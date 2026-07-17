namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated response body for <c>GET /api/v1/users/{id}</c>. Mirrors the API's
/// <c>UserDto</c> by value. <c>Status</c> is the <c>UserStatus</c> enum as a string
/// (e.g. "PendingVerification", "Active", "Disabled").
/// </summary>
public sealed record UserModel
{
    public Guid Id { get; init; }
    public string Phone { get; init; } = default!;
    public string? Email { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public bool PhoneVerified { get; init; }
}
