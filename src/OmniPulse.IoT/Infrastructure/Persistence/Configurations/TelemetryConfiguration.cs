using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.IoT.Domain.Entities;

namespace OmniPulse.IoT.Infrastructure.Persistence.Configurations;

public class TelemetryConfiguration : IEntityTypeConfiguration<Telemetry>
{
    public void Configure(EntityTypeBuilder<Telemetry> builder)
    {
        builder.ToTable("Telemetry");

        builder.HasKey(t => new { t.Id, t.Timestamp });

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.Property(t => t.DeviceId)
            .IsRequired();

        // Bir cihazın birden fazla telemetri kaydı olur 📈
        builder.HasOne(t => t.Device)
            .WithMany(d => d.Telemetries)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Temperature)
            .IsRequired();

        builder.Property(t => t.Pressure)
            .IsRequired();

        builder.Property(t => t.Timestamp)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(t => t.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);
    }
}
