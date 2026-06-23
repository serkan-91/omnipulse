using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniPulse.IoT.Domain.Entities;

namespace OmniPulse.IoT.Infrastructure.Persistence.Configurations;

public class AssetPermissionConfiguration : IEntityTypeConfiguration<AssetPermission>
{
    public void Configure(EntityTypeBuilder<AssetPermission> builder)
    {
        builder.ToTable("AssetPermissions");

        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.TenantId)
            .IsRequired();

        builder.Property(ap => ap.UserId)
            .IsRequired();

        builder.Property(ap => ap.Role)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(ap => ap.AssetId)
            .IsRequired();

        // Asset İlişkisi
        builder.HasOne(ap => ap.Asset)
            .WithMany(a => a.Permissions)
            .HasForeignKey(ap => ap.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ap => ap.CreatedAtUtc)
            .IsRequired();

        builder.Property(ap => ap.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(ap => ap.LastModifiedAtUtc)
            .IsRequired(false);

        builder.Property(ap => ap.LastModifiedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Birden fazla kişinin aynı rolde atanabilmesi için index benzersiz değildir
        builder.HasIndex(ap => new { ap.AssetId, ap.Role });

        // Kullanıcı bazlı sorgular için index
        builder.HasIndex(ap => ap.UserId);
        
        // Kiracı bazlı sorgular için index
        builder.HasIndex(ap => ap.TenantId);
    }
}
