using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.TenantModule.Domain.Entities;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("TenantUsers");

        // Composite Primary Key (TenantId ve UserId ikilisi benzersiz olmalı)
        builder.HasKey(tu => new { tu.TenantId, tu.UserId });

        builder.Property(tu => tu.Role)
            .IsRequired()
            .HasMaxLength(50);

        // İlişkilerin tanımlanması ve Cascade Delete ayarları
        builder.HasOne(tu => tu.Tenant)
            .WithMany()
            .HasForeignKey(tu => tu.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tu => tu.User)
            .WithMany(u => u.TenantUsers)
            .HasForeignKey(tu => tu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Denetim (Auditing) Alanları
        builder.Property(tu => tu.CreatedAtUtc)
            .IsRequired();

        builder.Property(tu => tu.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(tu => tu.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(tu => tu.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Soft Delete Alanları
        builder.Property(tu => tu.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(tu => tu.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(tu => tu.DeletedBy)
            .HasMaxLength(100)
            .IsRequired(false);
    }
}
