using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Tenant.Domain.Entities;

namespace OmniPulse.Tenant.Infrastructure.Persistence.Configurations;

public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
{
    public void Configure(EntityTypeBuilder<SecurityLog> builder)
    {
        builder.ToTable("SecurityLogs");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.TenantIdentifier)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(sl => sl.UserId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(sl => sl.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sl => sl.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sl => sl.IpAddress)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(sl => sl.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(sl => sl.IsSuccess)
            .IsRequired();

        builder.Property(sl => sl.FailureReason)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(sl => sl.TimestampUtc)
            .IsRequired();
    }
}
