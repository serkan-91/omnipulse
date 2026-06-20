using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.TenantModule.Domain.Entities;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(rt => rt.Token)
            .IsUnique();

        builder.Property(rt => rt.ExpiresAtUtc)
            .IsRequired();

        builder.Property(rt => rt.IsUsed)
            .HasDefaultValue(false);

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false);

        builder.Property(rt => rt.CreatedAtUtc)
            .IsRequired();

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(50)
            .IsRequired(false);

        // İlişki ayarları
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
