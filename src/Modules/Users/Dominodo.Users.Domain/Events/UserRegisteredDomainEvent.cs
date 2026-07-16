using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Phone, string? Email) : IDomainEvent;
