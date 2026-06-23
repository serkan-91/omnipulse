using System;
using System.Collections.Generic;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.IoT.Domain.Entities;

/// <summary>
/// Sahadaki fiziki IoT sensör donanımlarının kimlik kartı! 🔌🌡️
/// Sıcaklık, konum veya nem ölçen sensörler buradaki seri numarası (SerialNumber) ile eşleşir.
/// </summary>
public class Device : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }
    
    // Sensör adı (Örn: "Kasa Sıcaklık Sensörü-A")
    public string Name { get; private set; } = null!;
    
    // Cihazın benzersiz fabrika seri numarası (Örn: "SN-9872134-PANDA")
    public string SerialNumber { get; private set; } = null!;

    // Cihaz aktif mi (veri akışı açık mı)?
    public bool IsActive { get; private set; }

    // Bu sensörün bağlı olduğu varlık (araç, bant, dolap, vb.)
    public Guid? AssetId { get; private set; }
    public Asset? Asset { get; private set; }

    // Bu sensörün ait olduğu kategori
    public Guid? CategoryId { get; private set; }
    public DeviceCategory? Category { get; private set; }

    // Bu sensörden akan telemetri geçmişi
    public ICollection<Telemetry> Telemetries { get; private set; } = new List<Telemetry>();

    // IAuditableEntity - Denetim/İzleme alanları
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDelete - Güvenli/Yumuşak silme alanları
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // EF Core için boş constructor
    private Device() { }

    public static Device Create(
        Guid tenantId, 
        string name, 
        string serialNumber, 
        Guid? assetId = null, 
        Guid? categoryId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz şefim!", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cihaz adı boş bırakılamaz!", nameof(name));

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Seri numarası boş bırakılamaz!", nameof(serialNumber));

        return new Device
        {
            TenantId = tenantId,
            Name = name.Trim(),
            SerialNumber = serialNumber.ToUpperInvariant().Trim(),
            IsActive = true,
            AssetId = assetId,
            CategoryId = categoryId
        };
    }

    public void AssignToAsset(Guid? assetId)
    {
        AssetId = assetId;
    }

    public void AssignToCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
    }

    public void UpdateStatus(bool isActive)
    {
        IsActive = isActive;
    }

    public void UpdateDetails(string name, string serialNumber)
    {
        Name = name.Trim();
        SerialNumber = serialNumber.ToUpperInvariant().Trim();
    }
}
