using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.IoT.Domain.Entities;

namespace OmniPulse.IoT.Infrastructure.Persistence.Configurations;

public class AlarmEventConfiguration : IEntityTypeConfiguration<AlarmEvent>
{
    public void Configure(EntityTypeBuilder<AlarmEvent> builder)
    {
        builder.ToTable("AlarmEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.TriggeredValue)
            .IsRequired();

        builder.Property(e => e.ThresholdValue)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.TriggeredAtUtc)
            .IsRequired();

        builder.Property(e => e.IsResolved)
            .IsRequired();

        builder.Property(e => e.ResolvedAtUtc)
            .IsRequired(false);

        // Cihaz ve Kural ilişkileri
        builder.HasOne(e => e.Device)
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AlarmRule)
            .WithMany()
            .HasForeignKey(e => e.AlarmRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Auditing
        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.AlarmRuleId);
    }
}
