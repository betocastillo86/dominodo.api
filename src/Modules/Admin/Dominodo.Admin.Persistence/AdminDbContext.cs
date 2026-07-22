using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Devices;
using Dominodo.Admin.Domain.Notifications;
using Dominodo.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence;

internal sealed class AdminDbContext(DbContextOptions<AdminDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "admin";

    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<InAppMessage> InAppMessages => Set<InAppMessage>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<PushMessage> PushMessages => Set<PushMessage>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);

        // Wolverine's durable message storage (envelope tables) is provisioned by the bus in the
        // "wolverine" schema, not by this module's EF migrations (doc 07).
    }
}
