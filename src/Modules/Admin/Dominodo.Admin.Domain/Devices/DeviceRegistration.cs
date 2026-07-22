using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Devices;

// A push-device registration (domain-model §4.3). System-level: tied to the UserId, NOT to a tenant (a
// user carries their device across the conjuntos they belong to). Managed self-service by the owner.
public sealed class DeviceRegistration : AggregateRoot
{
    private DeviceRegistration() { } // EF Core

    private DeviceRegistration(
        Guid id,
        Guid userId,
        DevicePlatform platform,
        string token,
        bool isActive,
        DateTimeOffset updatedAtUtc) : base(id)
    {
        UserId = userId;
        Platform = platform;
        Token = token;
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid UserId { get; private set; }
    public DevicePlatform Platform { get; private set; }
    public string Token { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<DeviceRegistration> Register(
        Guid userId,
        DevicePlatform platform,
        string token,
        IClock clock)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation("DeviceRegistration.UserRequired", "A user is required.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("DeviceRegistration.TokenRequired", "A device token is required.");
        }

        return new DeviceRegistration(Guid.NewGuid(), userId, platform, token.Trim(), isActive: true, clock.UtcNow);
    }

    // Re-registration of the same token (e.g. after reinstall): refresh platform + reactivate.
    public void Reactivate(DevicePlatform platform, IClock clock)
    {
        Platform = platform;
        IsActive = true;
        UpdatedAtUtc = clock.UtcNow;
    }

    public void Deactivate(IClock clock)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = clock.UtcNow;
    }
}
