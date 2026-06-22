using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Identity.API.Services;

/// <summary>
/// IDistributedCache (Redis veya Memory) kullanarak JWT token iptal yönetimini gerçekleştiren servis! 🔐⛔
/// </summary>
public class TokenRevocationService(IDistributedCache cache) : ITokenRevocationService
{
    public async Task BlacklistTokenAsync(string jti, TimeSpan expiration)
    {
        if (string.IsNullOrEmpty(jti)) return;

        var key = $"blacklist:{jti}";
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        await cache.SetStringAsync(key, "revoked", options);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        if (string.IsNullOrEmpty(jti)) return false;

        var key = $"blacklist:{jti}";
        var value = await cache.GetStringAsync(key);
        
        return value != null;
    }

    public async Task RevokeUserAsync(string userId, TimeSpan duration)
    {
        if (string.IsNullOrEmpty(userId)) return;

        var key = $"revoked_user:{userId}";

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        };

        await cache.SetStringAsync(key, "revoked", options);
    }

    public async Task<bool> IsUserRevokedAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        var key = $"revoked_user:{userId}";
        var value = await cache.GetStringAsync(key);

        return value != null;
    }
}
