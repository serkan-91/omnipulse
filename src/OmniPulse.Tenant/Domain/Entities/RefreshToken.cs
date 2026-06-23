using System;
using OmniPulse.BuildingBlocks.Common;

namespace OmniPulse.Tenant.Domain.Entities;

/// <summary>
/// Kısa ömürlü JWT token'ların ömrü bittiğinde, kullanıcının hissetmeyeceği şekilde
/// yeni token üreten, tek kullanımlık ve rotasyonlu oturum koruma anahtarı! 🗝️🔄
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }

    // Gezinti (Navigation) Özelliği
    public User User { get; private set; } = null!;

    // EF Core için boş constructor
    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAtUtc, string? createdByIp)
    {
        if (userId == Guid.Empty) 
            throw new ArgumentException("Kullanıcı ID'si geçersiz şefim!", nameof(userId));
        
        if (string.IsNullOrWhiteSpace(token)) 
            throw new ArgumentException("Refresh token boş olamaz!", nameof(token));

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            IsUsed = false,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = createdByIp
        };
    }

    public void MarkAsUsed() => IsUsed = true;
    
    public void Revoke() => IsRevoked = true;
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    
    public bool IsActive => !IsUsed && !IsRevoked && !IsExpired;
}
