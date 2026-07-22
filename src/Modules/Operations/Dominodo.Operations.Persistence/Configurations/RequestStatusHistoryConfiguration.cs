using Dominodo.Operations.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("RequestStatusHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.RequestId).IsRequired();
        builder.Property(h => h.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(h => h.ToStatus).HasConversion<int>().IsRequired();
        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAtUtc).IsRequired();
        builder.Property(h => h.Note).HasMaxLength(500);

        builder.HasIndex(h => h.RequestId);
    }
}
