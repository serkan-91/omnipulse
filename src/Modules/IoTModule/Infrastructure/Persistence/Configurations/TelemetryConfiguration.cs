using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.IoTModule.Domain.Entities;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Configurations;

public class TelemetryConfiguration : IEntityTypeConfiguration<Telemetry>
{
    public void Configure(EntityTypeBuilder<Telemetry> builder)
    {
        builder.ToTable("Telemetry");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Temperature)
            .IsRequired();

        builder.Property(t => t.Pressure)
            .IsRequired();

        builder.Property(t => t.Timestamp)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
