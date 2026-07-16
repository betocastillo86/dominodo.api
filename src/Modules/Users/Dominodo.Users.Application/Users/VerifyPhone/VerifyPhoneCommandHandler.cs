using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Abstractions;
using Dominodo.Users.Domain.Authentication;
using Dominodo.Users.Domain.Ports;
using Microsoft.Extensions.Options;

namespace Dominodo.Users.Application.Users.VerifyPhone;

internal sealed class VerifyPhoneCommandHandler(
    IUserRepository users,
    IVerificationCodeRepository verificationCodes,
    IPasswordHasher passwordHasher,
    IClock clock,
    IOptions<OtpOptions> options)
    : ICommandHandler<VerifyPhoneCommand>
{
    private readonly OtpOptions _options = options.Value;

    public async Task<Result> Handle(VerifyPhoneCommand command, CancellationToken ct)
    {
        var verificationCode = await verificationCodes.GetLatestActiveAsync(
            command.Phone, VerificationPurpose.PhoneVerify, ct);

        if (verificationCode is null)
        {
            return Error.NotFound("Otp.NotFound", "No active verification code for this phone.");
        }

        // Read-only guards first (no mutation → nothing to persist on these failures).
        if (verificationCode.IsExpired(clock))
        {
            return Error.Conflict("Otp.Expired", "The verification code has expired.");
        }

        if (verificationCode.Attempts >= _options.MaxAttempts)
        {
            return Error.Conflict("Otp.TooManyAttempts", "Too many verification attempts.");
        }

        // Wrong code: record the attempt (persisted even though this returns a failure) then reject.
        if (!passwordHasher.Verify(command.Code, verificationCode.CodeHash))
        {
            verificationCode.RegisterAttempt();
            return Error.Validation("Otp.Invalid", "The verification code is invalid.");
        }

        var user = await users.GetByPhoneAsync(command.Phone, ct);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", "No user is registered with this phone.");
        }

        verificationCode.Consume(clock);

        var verify = user.VerifyPhone(clock);
        if (verify.IsFailure)
        {
            return verify.Error;
        }

        var activate = user.Activate();
        if (activate.IsFailure)
        {
            return activate.Error;
        }

        return Result.Success();
    }
}
