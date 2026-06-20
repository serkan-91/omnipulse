using System;
using System.Collections.Generic;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Modules.IoTModule.Domain.Entities;

/// <summary>
/// IoT cihazlarını mantıksal veya fiziksel olarak gruplamak için sınırsız hiyerarşik kategori ağacı! 🌳🏷️
/// </summary>
public class DeviceCategory : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }
    
    // Kategori adı (Örn: "Isı Cihazları", "Mutfak Sensörleri")
    public string Name { get; private set; } = null!;
    
    // Kategori açıklaması
    public string Description { get; private set; } = null!;

    // Ağaç yapısı için parent (üst) kategori referansı (Null ise kök kategoridir)
    public Guid? ParentCategoryId { get; private set; }
    public DeviceCategory? ParentCategory { get; private set; }

    // Bu kategorinin alt kırılımları (Alt kategoriler)
    public ICollection<DeviceCategory> SubCategories { get; private set; } = new List<DeviceCategory>();

    // Bu kategoriye atanmış sensörler
    public ICollection<Device> Devices { get; private set; } = new List<Device>();

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
    private DeviceCategory() { }

    public static DeviceCategory Create(Guid tenantId, string name, string description, Guid? parentCategoryId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz şefim!", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kategori adı boş bırakılamaz!", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Kategori açıklaması boş bırakılamaz!", nameof(description));

        return new DeviceCategory
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description.Trim(),
            ParentCategoryId = parentCategoryId
        };
    }

    public void UpdateDetails(string name, string description, Guid? parentCategoryId = null)
    {
        if (parentCategoryId == Id)
            throw new InvalidOperationException("Kategori kendisinin alt kategorisi olamaz sevgilim! 🪞");

        Name = name.Trim();
        Description = description.Trim();
        ParentCategoryId = parentCategoryId;
    }
}
