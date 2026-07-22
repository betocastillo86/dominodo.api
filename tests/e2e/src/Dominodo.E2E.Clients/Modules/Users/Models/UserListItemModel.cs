namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated item of the paged body for <c>GET /api/v1/users</c>. Mirrors the API's
/// <c>UserListItemDto</c> by value. <c>Status</c> and <c>DocumentType</c> are the corresponding
/// enums serialized as strings (e.g. "Active", "PendingVerification").
/// </summary>
public sealed record UserListItemModel
{
    public Guid Id { get; init; }
    public string Phone { get; init; } = default!;
    public string? Email { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? DocumentType { get; init; }
    public string? DocumentNumber { get; init; }
    public bool PhoneVerified { get; init; }
    public bool EmailVerified { get; init; }
}
