using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.TenantModule.Domain.Entities;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // Denetim (Auditing) Alanları
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(c => c.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(c => c.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Soft Delete Alanları
        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(c => c.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(c => c.DeletedBy)
            .HasMaxLength(100)
            .IsRequired(false);
    }
}
