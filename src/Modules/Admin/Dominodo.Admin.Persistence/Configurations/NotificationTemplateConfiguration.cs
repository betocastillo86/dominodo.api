using Dominodo.Admin.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId);
        builder.Property(t => t.Type).HasConversion<int>().IsRequired();
        builder.Property(t => t.EmailEnabled).IsRequired();
        builder.Property(t => t.PushEnabled).IsRequired();
        builder.Property(t => t.InAppEnabled).IsRequired();
        builder.Property(t => t.EmailSubject).HasMaxLength(300);
        builder.Property(t => t.EmailBodyHtml).HasColumnType("nvarchar(max)");
        builder.Property(t => t.InAppText).HasColumnType("nvarchar(max)");
        builder.Property(t => t.PushText).HasColumnType("nvarchar(max)");
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.Localization).HasColumnType("nvarchar(max)");

        // Unique per scope: one global default per type (TenantId null) and one override per (type, tenant).
        // HasFilter(null) removes EF's default null-exclusion so global rows are also protected.
        builder.HasIndex(t => new { t.Type, t.TenantId }).IsUnique().HasFilter(null);

        builder.Ignore(t => t.DomainEvents);
    }
}
