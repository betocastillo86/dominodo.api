namespace Dominodo.Users.Contracts.IntegrationEvents;

// Published when the Users module needs an OTP delivered. The Admin (Notifications) module consumes
// this and performs delivery (WhatsApp with email fallback). Users generates/stores/verifies the code.
// HasWhatsApp is the channel preference: true → deliver via WhatsApp; false → fall back to email.
public sealed record UserOtpRequestedIntegrationEvent(
    Guid EventId,
    Guid? UserId,
    string Phone,
    string? Email,
    string Code,
    string Purpose,
    bool HasWhatsApp);
