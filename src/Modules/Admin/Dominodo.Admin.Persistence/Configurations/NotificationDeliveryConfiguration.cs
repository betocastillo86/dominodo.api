using Dominodo.Admin.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.SourceEventId).IsRequired();
        builder.HasIndex(d => d.SourceEventId).IsUnique(); // idempotency guard

        builder.Property(d => d.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(d => d.Purpose).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.CreatedAtUtc).IsRequired();

        builder.Ignore(d => d.DomainEvents);
    }
}
