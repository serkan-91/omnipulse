using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.Modules.TenantModule.Domain.Entities;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
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
    }
}
