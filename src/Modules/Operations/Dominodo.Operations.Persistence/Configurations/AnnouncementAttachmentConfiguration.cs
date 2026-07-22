using Dominodo.Operations.Domain.Announcements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class AnnouncementAttachmentConfiguration : IEntityTypeConfiguration<AnnouncementAttachment>
{
    public void Configure(EntityTypeBuilder<AnnouncementAttachment> builder)
    {
        builder.ToTable("AnnouncementAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.AnnouncementId).IsRequired();
        builder.Property(a => a.FileUrl).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.UploadedByUserId).IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.AnnouncementId);
    }
}
