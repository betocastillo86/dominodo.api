using Dominodo.Operations.Persistence.Sequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class OperationSequenceConfiguration : IEntityTypeConfiguration<OperationSequence>
{
    public void Configure(EntityTypeBuilder<OperationSequence> builder)
    {
        builder.ToTable("OperationSequence");
        builder.HasKey(s => new { s.TenantId, s.Prefix, s.Year });

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.Prefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Year).IsRequired();
        builder.Property(s => s.Value).IsRequired();
    }
}
