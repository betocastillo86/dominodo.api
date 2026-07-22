using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmailMessageAggregate = Dominodo.Admin.Domain.Notifications.EmailMessage;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessageAggregate>
{
    public void Configure(EntityTypeBuilder<EmailMessageAggregate> builder)
    {
        builder.ToTable("EmailMessages");
        builder.HasKey(m => m.Id);

        // TenantId is a plain column (NOT ITenantOwned) — queried by recipient/status (§4.2).
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.To).HasMaxLength(256).IsRequired();
        builder.Property(m => m.ToName).HasMaxLength(200);
        builder.Property(m => m.Subject).HasMaxLength(300).IsRequired();
        builder.Property(m => m.BodyHtml).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(m => m.Priority).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Attempts).IsRequired();
        builder.Property(m => m.ScheduledAtUtc);
        builder.Property(m => m.SentAtUtc);

        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.Status);

        builder.Ignore(m => m.DomainEvents);
    }
}
