using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.TenantModule.Domain.Entities;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        // Sistem genelinde e-posta eşsiz olmalı!
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.AccessFailedCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.LockoutEnd)
            .IsRequired(false);

        // Denetim (Auditing) Alanları
        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(u => u.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(u => u.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Soft Delete Alanları
        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(u => u.DeletedBy)
            .HasMaxLength(100)
            .IsRequired(false);
    }
}
