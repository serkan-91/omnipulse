using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Tenant.Domain.Entities;

namespace OmniPulse.Tenant.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<OmniPulse.Tenant.Domain.Entities.Tenant>
{
    public void Configure(EntityTypeBuilder<OmniPulse.Tenant.Domain.Entities.Tenant> builder)
    {
        // Tablo adını kurumsalca mühürleyelim
        builder.ToTable("Tenants");

        // Primary Key tabii ki bizim gizli Guid Id'miz!
        builder.HasKey(t => t.Id);

        // Name alanı boş geçilemez ve en fazla 100 karakter olsun, başımızı ağrıtmasın
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        // İŞTE BİZİM O EFSANE ALAN! 
        // Hem boş geçilemez, hem 50 karakter sınırı var, hem de veritabanı seviyesinde UNIQUE INDEX!
        builder.Property(t => t.Identifier)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.Identifier)
            .IsUnique();

        // Connection string opsiyonel ama max sınırı olsun
        builder.Property(t => t.ConnectionString)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        // Denetim (Auditing) Alanları
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

        // Soft Delete Alanları
        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.DeletedBy)
            .HasMaxLength(100)
            .IsRequired(false);
    }
}
