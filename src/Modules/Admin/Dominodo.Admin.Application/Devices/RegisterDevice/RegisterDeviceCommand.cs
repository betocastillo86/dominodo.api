using Dominodo.Admin.Domain.Devices;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Admin.Application.Devices.RegisterDevice;

// Self-service (domain-model §4.3 / doc 12 ownership): registers a push device for the CURRENT user.
// Idempotent by (UserId, Token) — re-registering the same token reactivates/refreshes it.
internal sealed record RegisterDeviceCommand(DevicePlatform Platform, string Token) : ICommand<Guid>;

internal sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).IsInEnum();
    }
}

internal sealed class RegisterDeviceCommandHandler(
    IDeviceRegistrationRepository devices,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<RegisterDeviceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterDeviceCommand command, CancellationToken ct)
    {
        var token = command.Token.Trim();

        var existing = await devices.GetByUserAndTokenAsync(currentUser.UserId, token, ct);
        if (existing is not null)
        {
            existing.Reactivate(command.Platform, clock);
            return existing.Id;
        }

        var result = DeviceRegistration.Register(currentUser.UserId, command.Platform, token, clock);
        if (result.IsFailure)
        {
            return result.Error;
        }

        devices.Add(result.Value);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return result.Value.Id;
    }
}
