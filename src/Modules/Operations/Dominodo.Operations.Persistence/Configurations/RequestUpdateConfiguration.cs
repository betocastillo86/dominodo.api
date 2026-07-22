using Dominodo.Operations.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class RequestUpdateConfiguration : IEntityTypeConfiguration<RequestUpdate>
{
    public void Configure(EntityTypeBuilder<RequestUpdate> builder)
    {
        builder.ToTable("RequestUpdates");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.RequestId).IsRequired();
        builder.Property(u => u.AuthorUserId).IsRequired();
        builder.Property(u => u.Type).HasConversion<int>().IsRequired();
        builder.Property(u => u.Body).HasColumnType("nvarchar(max)");
        builder.Property(u => u.IsInternal).IsRequired();
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.HasIndex(u => u.RequestId);
    }
}
