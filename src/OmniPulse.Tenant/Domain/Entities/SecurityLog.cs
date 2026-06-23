using System;
using OmniPulse.BuildingBlocks.Common;

namespace OmniPulse.Tenant.Domain.Entities;

/// <summary>
/// Güvenlik ve kimlik doğrulama olaylarının kurumsal log kalesi! 🛡️🏰
/// </summary>
public class SecurityLog : BaseEntity
{
    // Hangi kiracı altında bu işlem yapıldı
    public string? TenantIdentifier { get; private set; }

    // Eğer başarılı olduysa, etkilenen kullanıcının Id'si
    public string? UserId { get; private set; }

    // Giriş yapılmaya çalışılan kullanıcı adı veya email adresi
    public string Username { get; private set; } = null!;

    // Yapılan güvenlik eylemi (Örn: "LoginSuccess", "LoginFailed", "TokenRefresh", "Logout")
    public string Action { get; private set; } = null!;

    // İsteğin geldiği IP adresi
    public string? IpAddress { get; private set; }

    // Tarayıcı ve sistem bilgisi
    public string? UserAgent { get; private set; }

    // İşlem başarılı mı
    public bool IsSuccess { get; private set; }

    // Eğer işlem başarısızsa hata sebebi (Örn: "InvalidPassword", "UserBlocked")
    public string? FailureReason { get; private set; }

    // Logun atıldığı UTC zaman damgası
    public DateTime TimestampUtc { get; private set; }

    // EF Core için boş constructor
    private SecurityLog() { }

    public static SecurityLog Create(
        string? tenantIdentifier,
        string? userId,
        string username,
        string action,
        string? ipAddress,
        string? userAgent,
        bool isSuccess,
        string? failureReason = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Kullanıcı adı boş bırakılamaz şefim!", nameof(username));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Güvenlik eylemi boş bırakılamaz!", nameof(action));

        return new SecurityLog
        {
            TenantIdentifier = tenantIdentifier?.Trim(),
            UserId = userId,
            Username = username.Trim(),
            Action = action.Trim(),
            IpAddress = ipAddress?.Trim(),
            UserAgent = userAgent?.Trim(),
            IsSuccess = isSuccess,
            FailureReason = failureReason?.Trim(),
            TimestampUtc = DateTime.UtcNow
        };
    }
}
