using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Tenant.Domain.Entities;
using OmniPulse.Tenant.Infrastructure.Persistence;

namespace OmniPulse.Tenant.Features.Auth.Logout;

public class LogoutCommandHandler(
    IdentityDbContext dbContext,
    ITokenRevocationService tokenRevocationService
) : IRequestHandler<LogoutCommand, LogoutResponse>
{
    public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Jti))
        {
            return new LogoutResponse(false, "Token ID (JTI) bulunamadı.");
        }

        // Kalan süreyi hesapla
        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(request.ExpirySeconds);
        var remainingTime = expirationTime - DateTimeOffset.UtcNow;

        // Eğer token süresi çoktan geçmişse veya sıfırın altındaysa 1 saniye veya 5 saniye gibi sembolik bir süre blacklist yapalım
        if (remainingTime.TotalSeconds <= 0)
        {
            remainingTime = TimeSpan.FromSeconds(5);
        }

        // Token'ı blacklist'e al! 🔐⛔
        await tokenRevocationService.BlacklistTokenAsync(request.Jti, remainingTime);

        // Güvenlik logunu kaydet
        var securityLog = SecurityLog.Create(
            request.TenantIdentifier,
            request.UserId,
            request.Email ?? "Unknown",
            "Logout",
            request.IpAddress,
            request.UserAgent,
            true,
            null
        );

        dbContext.SecurityLogs.Add(securityLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LogoutResponse(true, "Oturum başarıyla kapatıldı.");
    }
}
