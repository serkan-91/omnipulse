using System;
using System.Threading.Tasks;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Çalınan, iptal edilen veya süresi dolan token'ların (JTI üzerinden)
/// blacklist yönetiminden sorumlu dağıtık arayüz! 🔐⛔
/// </summary>
public interface ITokenRevocationService
{
    // Token'ı (jti değerini) kalan süresi kadar blacklist'e ekler
    Task BlacklistTokenAsync(string jti, TimeSpan expiration);

    // Token'ın (jti değerini) blacklist'te olup olmadığını sorgular
    Task<bool> IsTokenBlacklistedAsync(string jti);

    // Kullanıcıyı (userId değerini) belirli bir süre (örn. token max süresi) sistem genelinde geçersiz kılar 🚫
    Task RevokeUserAsync(string userId, TimeSpan duration);

    // Kullanıcının yetkilerinin/oturumunun iptal edilip edilmediğini sorgular
    Task<bool> IsUserRevokedAsync(string userId);
}
