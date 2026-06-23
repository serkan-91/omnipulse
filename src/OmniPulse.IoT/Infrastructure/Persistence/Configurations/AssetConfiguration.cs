using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.IoT.Domain.Entities;

namespace OmniPulse.IoT.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        // AssetType artık serbest string olarak tutulur
        builder.Property(a => a.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.MetadataJson)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(a => a.ResponsibleUserId)
            .IsRequired(false);

        builder.Property(a => a.ResponsibleRole)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(a => a.ParentAssetId)
            .IsRequired(false);

        // Self-referencing hiyerarşi: Üst varlık → Alt varlıklar
        builder.HasOne(a => a.ParentAsset)
            .WithMany(a => a.Children)
            .HasForeignKey(a => a.ParentAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cihaz ilişkisi: Orphan sensörü önlemek için Restrict
        builder.HasMany(a => a.Devices)
            .WithOne(d => d.Asset)
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(a => a.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Kiracı + Tür kombinasyonu için indeks
        builder.HasIndex(a => new { a.TenantId, a.Type });

        // Sorumlu kullanıcı bazlı aramalar için indeks
        builder.HasIndex(a => a.ResponsibleUserId);

        // Hiyerarşik sorgular için üst varlık indeksi
        builder.HasIndex(a => a.ParentAssetId);
    }
}
