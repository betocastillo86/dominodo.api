using Dominodo.Users.Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("VerificationCodes");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Phone).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Purpose).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(v => v.CodeHash).HasMaxLength(200).IsRequired();
        builder.Property(v => v.ExpiresAtUtc).IsRequired();
        builder.Property(v => v.Attempts).IsRequired();

        builder.HasIndex(v => new { v.Phone, v.Purpose });

        builder.Ignore(v => v.DomainEvents);
    }
}
