using Dominodo.Users.Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.TokenHash).HasMaxLength(200).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);

        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.DomainEvents);
    }
}
