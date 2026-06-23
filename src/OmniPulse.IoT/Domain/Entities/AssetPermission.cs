using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.IoT.Domain.Entities;

/// <summary>
/// Varlık bazlı yetkilendirme ve rol atama modeli! 🛡️🏭
/// Bir varlığa (Bant, Sektör, Araç vb.) birden fazla kişi atanabilir, 
/// ancak aynı rolde (örn: Operator, ShiftSupervisor) sadece bir kişi olabilir.
/// </summary>
public class AssetPermission : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }

    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;

    public Guid UserId { get; private set; }
    
    // Rol adı (örn: "Operator", "ShiftSupervisor", "QualityControl")
    public string Role { get; private set; } = null!;

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
    private AssetPermission() { }

    public static AssetPermission Create(
        Guid tenantId,
        Guid assetId,
        Guid userId,
        string role)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz!", nameof(tenantId));

        if (assetId == Guid.Empty)
            throw new ArgumentException("Varlık ID'si boş bırakılamaz!", nameof(assetId));

        if (userId == Guid.Empty)
            throw new ArgumentException("Kullanıcı ID'si boş bırakılamaz!", nameof(userId));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Rol adı boş bırakılamaz!", nameof(role));

        return new AssetPermission
        {
            TenantId = tenantId,
            AssetId = assetId,
            UserId = userId,
            Role = role.Trim()
        };
    }

    public void UpdateUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Kullanıcı ID'si boş bırakılamaz!", nameof(userId));
        
        UserId = userId;
    }
}
