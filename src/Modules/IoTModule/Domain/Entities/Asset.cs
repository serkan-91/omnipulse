using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Modules.IoTModule.Domain.Entities;

/// <summary>
/// Sektör-bağımsız evrensel varlık modeli! 🏭🚛🌆
/// Tır filosu, fabrika bantları, laboratuvar dolapları ve akıllı şehir altyapısı
/// aynı entity altında yönetilebilir.
/// </summary>
public class Asset : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }

    // Varlık adı (Örn: "Soğutmalı Tır-7", "Konveyör Bant-A3", "Fridge-04")
    public string Name { get; private set; } = null!;

    // Varlık türü (sektör belirleyici) — serbest string olarak tutulur
    public string Type { get; private set; } = null!;

    // Hiyerarşik ağaç desteği: Üst varlık (fabrika → bant → eksen gibi)
    public Guid? ParentAssetId { get; private set; }
    public Asset? ParentAsset { get; private set; }

    // Alt varlıklar (self-referencing tree)
    public ICollection<Asset> Children { get; private set; } = new List<Asset>();

    // Sorumlu kullanıcı: sürücü, operatör, laborant — kim sorumluysa
    public Guid? ResponsibleUserId { get; private set; }

    /// <summary>
    /// Mantıksal rol adı (örn: MAINTENANCE_ROLE, OPERATOR_ROLE).
    /// Öncelik: ResponsibleUserId (override) > ResponsibleRole > FallbackUserId.
    /// </summary>
    public string? ResponsibleRole { get; private set; }

    // Sektörel özellikler için esnek JSON alanı
    // Örn: plakalı araç için {"plateNumber":"34ABC"}, fabrika için {"rpmLimit":1500}
    public string? MetadataJson { get; private set; }

    // Bu varlığa bağlı sensörler/cihazlar
    public ICollection<Device> Devices { get; private set; } = new List<Device>();

    // Bu varlığa ait yetki/rol atamaları
    public ICollection<AssetPermission> Permissions { get; private set; } = new List<AssetPermission>();

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
    private Asset() { }

    public static Asset Create(
        Guid tenantId,
        string name,
        string type,
        Guid? parentAssetId = null,
        Guid? responsibleUserId = null,
        string? metadataJson = null,
        string? responsibleRole = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz!", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Varlık adı boş bırakılamaz!", nameof(name));

        return new Asset
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Type = type,
            ParentAssetId = parentAssetId,
            ResponsibleUserId = responsibleUserId,
            MetadataJson = metadataJson,
            ResponsibleRole = responsibleRole
        };
    }

    public void AssignResponsibleUser(Guid? userId)
    {
        ResponsibleUserId = userId;
    }

    /// <summary>Asset'e mantıksal rol atar (Role Aliasing için).</summary>
    public void AssignRole(string? role) => ResponsibleRole = role;

    public void UpdateMetadata(string? metadataJson)
    {
        MetadataJson = metadataJson;
    }

    public void UpdateDetails(string name, string type)
    {
        Name = name.Trim();
        Type = type;
    }
}
