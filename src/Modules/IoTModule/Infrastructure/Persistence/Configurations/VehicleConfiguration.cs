using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.IoTModule.Domain.Entities;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId)
            .IsRequired();

        builder.Property(v => v.PlateNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.DriverUserId)
            .IsRequired(false);

        // Bir tırın birden fazla sensörü olabilir
        builder.HasMany(v => v.Devices)
            .WithOne(d => d.Vehicle)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(v => v.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(v => v.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Sorgularda plaka ile hızlı arama için indeks ekliyoruz
        builder.HasIndex(v => new { v.TenantId, v.PlateNumber })
            .IsUnique();

        // Sürücü bazlı aramalar için indeks ekliyoruz
        builder.HasIndex(v => v.DriverUserId);
    }
}
