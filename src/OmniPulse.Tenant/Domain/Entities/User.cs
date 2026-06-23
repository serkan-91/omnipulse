using System;
using System.Collections.Generic;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Tenant.Domain.Entities;

/// <summary>
/// Sistemdeki tüm kullanıcıların global kimlik kartı! 🧑‍💻
/// Bir kullanıcı birden fazla kiracıya (Tenant) üye olabilir.
/// </summary>
public class User : BaseEntity, IAuditableEntity, ISoftDelete
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; }

    // Account Lockout Alanları (Microsoft/Google Güvenlik Hassasiyeti) 🛡️
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }

    // Çoka-çok ilişki için gezinti (navigation) özelliği
    public ICollection<TenantUser> TenantUsers { get; private set; } = new List<TenantUser>();

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
    protected User() { }

    public static User Create(string firstName, string lastName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(firstName)) 
            throw new ArgumentException("Kullanıcı adı boş bırakılamaz şefim!", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName)) 
            throw new ArgumentException("Kullanıcı soyadı boş bırakılamaz!", nameof(lastName));
        
        if (string.IsNullOrWhiteSpace(email)) 
            throw new ArgumentException("E-posta adresi boş bırakılamaz!", nameof(email));
        
        if (string.IsNullOrWhiteSpace(passwordHash)) 
            throw new ArgumentException("Şifre özeti boş bırakılamaz!", nameof(passwordHash));

        return new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            IsActive = true
        };
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void IncrementAccessFailedCount()
    {
        AccessFailedCount++;
        if (AccessFailedCount >= 5)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(15); // 5 hatalı denemede 15 dk ceza!
        }
    }

    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}
