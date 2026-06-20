using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.IoTModule.Domain.Entities;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Configurations;

public class DeviceCategoryConfiguration : IEntityTypeConfiguration<DeviceCategory>
{
    public void Configure(EntityTypeBuilder<DeviceCategory> builder)
    {
        builder.ToTable("DeviceCategories");

        builder.HasKey(dc => dc.Id);

        builder.Property(dc => dc.TenantId)
            .IsRequired();

        builder.Property(dc => dc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(dc => dc.Description)
            .HasMaxLength(250)
            .IsRequired();

        // Sihirli self-referencing (kendi kendini işaret eden ağaç yapısı!) 🌳
        builder.HasOne(dc => dc.ParentCategory)
            .WithMany(dc => dc.SubCategories)
            .HasForeignKey(dc => dc.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Kök kategori silindiğinde altındakiler korunsun

        builder.Property(dc => dc.CreatedAtUtc)
            .IsRequired();

        builder.Property(dc => dc.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(dc => dc.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(dc => dc.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Bir kiracının kategorileri içinde benzersiz aramalar için indeks
        builder.HasIndex(dc => new { dc.TenantId, dc.Name });
    }
}
