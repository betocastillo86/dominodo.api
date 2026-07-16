using Dominodo.Users.Domain.Users;
using Dominodo.Users.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Phone).HasMaxLength(20).IsRequired();
        builder.HasIndex(u => u.Phone).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();

        builder.Property(u => u.DocumentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.DocumentNumber).HasMaxLength(50);
        builder.HasIndex(u => u.DocumentNumber);

        builder.Property(u => u.PasswordHash).HasMaxLength(200);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(u => u.PreferredLanguage).HasMaxLength(10).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.Profile).HasColumnType("nvarchar(max)");

        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        builder.Ignore(u => u.DomainEvents);

        // Bootstrap SuperAdmin (domain-model §1; plan Phase 2). Verified + active from the start.
        builder.HasData(new
        {
            Id = UsersSeedData.SuperAdminUserId,
            Phone = UsersSeedData.SuperAdminPhone,
            Email = UsersSeedData.SuperAdminEmail,
            FirstName = "Super",
            LastName = "Admin",
            PasswordHash = UsersSeedData.SuperAdminPasswordHash,
            Status = UserStatus.Active,
            PhoneVerifiedAtUtc = (DateTimeOffset?)new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PreferredLanguage = "es",
            CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
