using Dominodo.Operations.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class RequestParticipantConfiguration : IEntityTypeConfiguration<RequestParticipant>
{
    public void Configure(EntityTypeBuilder<RequestParticipant> builder)
    {
        builder.ToTable("RequestParticipants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.RequestId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.ParticipantType).HasConversion<int>().IsRequired();
        builder.Property(p => p.Source).HasConversion<int>().IsRequired();
        builder.Property(p => p.JoinedAtUtc).IsRequired();

        // One participant row per (request, user).
        builder.HasIndex(p => new { p.RequestId, p.UserId }).IsUnique();
    }
}
