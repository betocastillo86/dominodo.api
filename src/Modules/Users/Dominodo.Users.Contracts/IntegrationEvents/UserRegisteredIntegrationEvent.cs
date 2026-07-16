namespace Dominodo.Users.Contracts.IntegrationEvents;

public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Phone,
    string? Email,
    string FirstName,
    string LastName);
