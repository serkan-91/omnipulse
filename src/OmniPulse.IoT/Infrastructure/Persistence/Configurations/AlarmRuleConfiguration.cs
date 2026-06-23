using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.IoT.Domain.Entities;

namespace OmniPulse.IoT.Infrastructure.Persistence.Configurations;

public class AlarmRuleConfiguration : IEntityTypeConfiguration<AlarmRule>
{
    public void Configure(EntityTypeBuilder<AlarmRule> builder)
    {
        builder.ToTable("AlarmRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.MetricKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.ThresholdValue)
            .IsRequired();

        builder.Property(r => r.ComparisonOperator)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.IsActive)
            .IsRequired();

        // Bir cihazın birden fazla alarm kuralı olabilir
        builder.HasOne(r => r.Device)
            .WithMany()
            .HasForeignKey(r => r.DeviceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Auditing
        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(r => r.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(r => r.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.DeviceId);
    }
}
