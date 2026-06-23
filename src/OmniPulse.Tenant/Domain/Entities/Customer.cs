using System;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Tenant.Domain.Entities;

/// <summary>
/// Müşteri bilgilerini barındıran domain varlığı! 👥
/// </summary>
public class Customer : BaseEntity, IAuditableEntity, ISoftDelete, ITenantEntity
{
    public Guid TenantId { get; set; } // Kiracıyı bağlayan sihirli alan!
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool IsActive { get; private set; }

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
    private Customer() { }

    // Yeni müşteri eklemek için temiz bir constructor ve encapsulation
    public Customer(Guid tenantId, string firstName, string lastName, string email)
    {
        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        IsActive = true;
    }

    // Müşteri bilgilerini güncellemek istersek yaramaz metotlar ;)
    public void UpdateDetails(string firstName, string lastName, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}