using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Events;

// Raised when a phone OTP is requested. Carries the PLAINTEXT code for in-process delivery only
// (never persisted — the VerificationCode aggregate stores only the hash). A notification handler
// translates it into the UserOtpRequestedIntegrationEvent published to the Admin module.
public sealed record PhoneVerificationRequestedDomainEvent(
    Guid UserId,
    string Phone,
    string? Email,
    string Code,
    bool HasWhatsApp) : IDomainEvent;
