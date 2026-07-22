using Dominodo.Admin.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class PushMessageConfiguration : IEntityTypeConfiguration<PushMessage>
{
    public void Configure(EntityTypeBuilder<PushMessage> builder)
    {
        builder.ToTable("PushMessages");
        builder.HasKey(m => m.Id);

        // TenantId is a plain column (NOT ITenantOwned) — queried by recipient/status (§4.2).
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.RecipientUserId).IsRequired();
        builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(m => m.TargetUrl).HasMaxLength(2048);
        builder.Property(m => m.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Attempts).IsRequired();
        builder.Property(m => m.DedupHash).HasMaxLength(128).IsRequired();
        builder.Property(m => m.SentAtUtc);

        builder.HasIndex(m => m.RecipientUserId);
        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.Status);

        builder.Ignore(m => m.DomainEvents);
    }
}
