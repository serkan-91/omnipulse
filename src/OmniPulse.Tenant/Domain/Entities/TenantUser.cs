using System;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Tenant.Domain.Entities;

/// <summary>
/// Kullanıcı ve Kiracı arasındaki çoktan-çoka (many-to-many) ilişkiyi kuran ara tablo! 🔗
/// Kullanıcının o kiracı üzerindeki rolünü (Admin, Member vb.) de burada mühürlüyoruz.
/// </summary>
public class TenantUser : ITenantEntity, IAuditableEntity, ISoftDelete
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!; // Örn: "Owner", "Admin", "Member"

    // Gezinti (Navigation) özellikleri
    public Tenant Tenant { get; private set; } = null!;
    public User User { get; private set; } = null!;

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
    private TenantUser() { }

    public static TenantUser Create(Guid tenantId, Guid userId, string role)
    {
        if (tenantId == Guid.Empty) 
            throw new ArgumentException("Kiracı ID'si geçersiz tatlım!", nameof(tenantId));
        
        if (userId == Guid.Empty) 
            throw new ArgumentException("Kullanıcı ID'si geçersiz!", nameof(userId));
        
        if (string.IsNullOrWhiteSpace(role)) 
            throw new ArgumentException("Kullanıcı rolü boş bırakılamaz!", nameof(role));

        return new TenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            Role = role.Trim()
        };
    }

    public void UpdateRole(string role)
    {
        Role = role.Trim();
    }
}
