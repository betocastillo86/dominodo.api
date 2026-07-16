using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Abstractions;
using Dominodo.Users.Application.Security;
using Dominodo.Users.Contracts.IntegrationEvents;
using Dominodo.Users.Domain.Authentication;
using Dominodo.Users.Domain.Ports;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Dominodo.Users.Application.Users.RequestPhoneVerification;

internal sealed class RequestPhoneVerificationCommandHandler(
    IUserRepository users,
    IVerificationCodeRepository verificationCodes,
    IPasswordHasher passwordHasher,
    IClock clock,
    IMessageBus bus,
    IOptions<OtpOptions> options)
    : ICommandHandler<RequestPhoneVerificationCommand>
{
    private readonly OtpOptions _options = options.Value;

    public async Task<Result> Handle(RequestPhoneVerificationCommand command, CancellationToken ct)
    {
        var user = await users.GetByPhoneAsync(command.Phone, ct);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", "No user is registered with this phone.");
        }

        var code = OtpCode.Generate(_options.Length);

        // Phone is the WhatsApp channel key (domain-model §1.1), so HasWhatsApp defaults to true.
        const bool hasWhatsApp = true;

        // Guard (e.g. already verified → Conflict); also records the domain intent.
        var request = user.RequestPhoneVerification(code, hasWhatsApp);
        if (request.IsFailure)
        {
            return request.Error;
        }

        var codeHash = passwordHasher.Hash(code);
        verificationCodes.Add(VerificationCode.Issue(
            user.Id,
            user.Phone,
            VerificationPurpose.PhoneVerify,
            codeHash,
            clock.UtcNow.AddMinutes(_options.TtlMinutes)));

        // Deliver the OTP through the Admin module (ADR-0007). PublishAsync routes to Wolverine's
        // durable local queue (persisted to the "wolverine" store), delivered to the Admin handler.
        await bus.PublishAsync(
            new UserOtpRequestedIntegrationEvent(
                EventId: Guid.NewGuid(),
                UserId: user.Id,
                Phone: user.Phone,
                Email: user.Email,
                Code: code,
                Purpose: "PhoneVerify",
                HasWhatsApp: hasWhatsApp));

        return Result.Success();
    }
}
