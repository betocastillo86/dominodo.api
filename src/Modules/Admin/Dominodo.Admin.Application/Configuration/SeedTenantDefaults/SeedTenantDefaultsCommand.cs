using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Configuration.SeedTenantDefaults;

// Provisions a newly-created conjunto's default Admin config + templates (domain-model §5.3). Carries the
// TenantId explicitly — this runs from the TenantCreated consumer, not an HTTP request, so there is no
// ITenantContext. Idempotent: each row is guarded by an Exists check (and the unique (Key,TenantId) /
// (Type,TenantId) indexes), so re-delivery of the event is a no-op (doc 11).
internal sealed record SeedTenantDefaultsCommand(Guid TenantId) : ICommand;

internal sealed class SeedTenantDefaultsCommandHandler(
    ISystemSettingRepository settings,
    INotificationTemplateRepository templates,
    IClock clock)
    : ICommandHandler<SeedTenantDefaultsCommand>
{
    // Default per-tenant SystemSetting overrides seeded on conjunto creation.
    private static readonly (string Key, string Value, SystemSettingValueType Type)[] DefaultSettings =
    [
        ("notifications.locale", "\"es\"", SystemSettingValueType.String),
        ("notifications.email.enabled", "true", SystemSettingValueType.Bool),
    ];

    public async Task<Result> Handle(SeedTenantDefaultsCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty)
        {
            return Error.Validation("SeedTenantDefaults.TenantRequired", "A tenant is required.");
        }

        foreach (var (key, value, type) in DefaultSettings)
        {
            if (await settings.ExistsAsync(key, command.TenantId, ct))
            {
                continue;
            }

            var result = SystemSetting.Create(key, command.TenantId, value, type, clock);
            if (result.IsFailure)
            {
                return result.Error;
            }

            settings.Add(result.Value);
        }

        // Default Welcome template override for the tenant (falls back to the global default until edited).
        if (!await templates.ExistsAsync(NotificationType.Welcome, command.TenantId, ct))
        {
            var welcome = NotificationTemplate.Create(
                command.TenantId,
                NotificationType.Welcome,
                NotificationChannel.Email | NotificationChannel.InApp);

            welcome.UpdateContent(
                NotificationChannel.Email | NotificationChannel.InApp,
                emailSubject: "¡Bienvenido a tu conjunto!",
                emailBodyHtml: "<p>Te damos la bienvenida a la plataforma de tu conjunto.</p>",
                inAppText: "¡Bienvenido a tu conjunto!",
                pushText: null,
                isActive: true,
                localization: null);

            templates.Add(welcome);
        }

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
