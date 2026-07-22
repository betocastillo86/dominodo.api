using Dominodo.Operations.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.ToTable("RequestAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.RequestId).IsRequired();
        builder.Property(a => a.RequestUpdateId);
        builder.Property(a => a.FileUrl).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.UploadedByUserId).IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.RequestId);
    }
}
