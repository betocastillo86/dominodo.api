namespace Dominodo.Users.Contracts;

public sealed record UserDto(
    Guid Id,
    string Phone,
    string? Email,
    string FirstName,
    string LastName,
    string Status,
    bool PhoneVerified);
