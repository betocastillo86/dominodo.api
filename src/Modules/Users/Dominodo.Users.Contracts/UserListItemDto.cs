namespace Dominodo.Users.Contracts;

public sealed record UserListItemDto(
    Guid Id,
    string Phone,
    string? Email,
    string FirstName,
    string LastName,
    string Status,
    string? DocumentType,
    string? DocumentNumber,
    bool PhoneVerified,
    bool EmailVerified);
