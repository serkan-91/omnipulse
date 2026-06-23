using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Tenant.Domain.Entities;
using OmniPulse.Tenant.Infrastructure.Persistence;

namespace OmniPulse.Tenant.Features.Auth.Refresh;

/// <summary>
/// Refresh Token tazeleyici komut işleyicisi (Command Handler) 🔄🗝️
/// Güvenlik açısından token rotasyonu (Token Rotation) ve mükerrer kullanım algılama (Replay Attack Prevention) yapar.
/// </summary>
public class RefreshCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService)
    : IRequestHandler<RefreshCommand, RefreshResponse>
{
    public async Task<RefreshResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var genericErrorMessage = "Geçersiz oturum tazeleme talebi şefim.";

        // 1. JWT token'dan iddiaları (claims) söküyoruz
        var principal = tokenService.GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
        {
            return new RefreshResponse(false, null, null, genericErrorMessage);
        }

        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        var tenantIdStr = principal.FindFirst("tid")?.Value 
                          ?? principal.FindFirst("tenant_id")?.Value
                          ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(tenantIdStr) ||
            !Guid.TryParse(userIdStr, out var userId) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return new RefreshResponse(false, null, null, genericErrorMessage);
        }

        // 2. Veri tabanında bu refresh token'ı arıyoruz
        var refreshToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        // Güvenlik Önlemi: Token bulunamadıysa veya başka bir kullanıcıya aitse hata ver
        if (refreshToken == null || refreshToken.UserId != userId)
        {
            return new RefreshResponse(false, null, null, genericErrorMessage);
        }

        // 3. REPLAY ATTACK (Mükerrer Kullanım) ALGILAYICI 🚨
        // Eğer token daha önce kullanılmışsa, bu bir saldırı belirtisi olabilir (token çalınması).
        // Kullanıcının sistemdeki tüm aktif refresh token'larını hemen iptal ediyoruz!
        if (refreshToken.IsUsed)
        {
            var activeTokens = await dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsUsed && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke();
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await LogSecurityEvent(
                tenantId.ToString(),
                userId.ToString(),
                refreshToken.User.Email,
                "RefreshTokenReplayAttack",
                request.IpAddress,
                request.UserAgent,
                false,
                "ReusedRefreshTokenRevokedAll",
                cancellationToken
            );

            return new RefreshResponse(false, null, null, "Güvenlik ihlali şüphesiyle tüm oturumlarınız kapatıldı. Lütfen tekrar giriş yapın.");
        }

        // 4. Token'ın süresi geçmiş veya iptal edilmiş mi?
        if (!refreshToken.IsActive)
        {
            return new RefreshResponse(false, null, null, "Oturum süresi dolmuş veya iptal edilmiş.");
        }

        // 5. Kullanıcı durumu ve kilit kontrolü
        var user = refreshToken.User;
        if (user.IsDeleted || !user.IsActive)
        {
            return new RefreshResponse(false, null, null, "Hesabınız pasif duruma getirilmiş.");
        }

        if (user.IsLockedOut)
        {
            return new RefreshResponse(false, null, null, "Hesabınız kilitli durumdadır.");
        }

        // 6. Kiracı (Tenant) kontrolü ve yetki/rol doğrulama
        var tenantUser = await dbContext.TenantUsers
            .Include(tu => tu.Tenant)
            .FirstOrDefaultAsync(tu => tu.UserId == userId && tu.TenantId == tenantId && !tu.IsDeleted, cancellationToken);

        if (tenantUser == null || !tenantUser.Tenant.IsActive || tenantUser.Tenant.IsDeleted)
        {
            return new RefreshResponse(false, null, null, "Bu şirketteki üyeliğiniz aktif değil veya şirket askıya alınmış.");
        }

        // 7. Mevcut Refresh Token'ı kullanıldı olarak işaretle
        refreshToken.MarkAsUsed();

        // 8. Yeni JWT ve Rotasyonlu yeni Refresh Token üret
        var newToken = tokenService.GenerateToken(
            user.Id.ToString(),
            user.Email,
            tenantUser.TenantId,
            tenantUser.Tenant.Identifier,
            new[] { tenantUser.Role }
        );

        var newRefreshTokenValue = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenValue, DateTime.UtcNow.AddDays(7), request.IpAddress);
        
        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await LogSecurityEvent(
            tenantUser.Tenant.Identifier,
            user.Id.ToString(),
            user.Email,
            "TokenRefreshed",
            request.IpAddress,
            request.UserAgent,
            true,
            null,
            cancellationToken
        );

        return new RefreshResponse(true, newToken, newRefreshTokenValue, "Oturum başarıyla tazelendi.");
    }

    private async Task LogSecurityEvent(
        string? tenantIdentifier,
        string? userId,
        string username,
        string action,
        string? ipAddress,
        string? userAgent,
        bool isSuccess,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var securityLog = SecurityLog.Create(
            tenantIdentifier,
            userId,
            username,
            action,
            ipAddress,
            userAgent,
            isSuccess,
            failureReason
        );

        dbContext.SecurityLogs.Add(securityLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
