using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Events;

public sealed record UserPhoneVerifiedDomainEvent(Guid UserId, DateTimeOffset VerifiedAtUtc) : IDomainEvent;
