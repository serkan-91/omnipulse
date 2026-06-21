using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.IoTModule.Domain.Entities;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.PartitionKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.Property(o => o.AttemptCount)
            .IsRequired();

        builder.Property(o => o.LastError)
            .HasMaxLength(1000)
            .IsRequired(false);
    }
}
