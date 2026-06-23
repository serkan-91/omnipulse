using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.IoT.Domain.Entities;

namespace OmniPulse.IoT.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId)
            .IsRequired();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.SerialNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Bir cihaz/sensör sadece bir varlığa bağlı olabilir (araç, bant, dolap, vb.)
        builder.HasOne(d => d.Asset)
            .WithMany(a => a.Devices)
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir cihaz/sensör sadece bir kategoride olabilir
        builder.HasOne(d => d.Category)
            .WithMany(c => c.Devices)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(d => d.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(d => d.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Bir kiracının (şirketin) cihaz seri numaraları benzersiz olmalıdır
        builder.HasIndex(d => new { d.TenantId, d.SerialNumber })
            .IsUnique();
    }
}
