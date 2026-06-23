using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Tenant.Domain.Entities;
using OmniPulse.Tenant.Infrastructure.Persistence;

namespace OmniPulse.Tenant.Features.Auth.Login;

/// <summary>
/// Giriş denemesi komut işleyicisi (Command Handler) 🚀
/// Amazon/Microsoft standartlarında timing-attack, user-enumeration saldırılarını önler ve denetim loglarını tutar.
/// </summary>
public class LoginCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var genericErrorMessage = "E-posta veya şifre hatalı şefim.";

        // 0. IP Adresi Kilitli mi? (IP Lockout Policy) 🛡️
        if (!string.IsNullOrEmpty(request.IpAddress))
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-15);
            var failedAttemptsFromIp = await dbContext.SecurityLogs
                .Where(log => log.IpAddress == request.IpAddress 
                              && log.TimestampUtc >= cutoffTime 
                              && !log.IsSuccess)
                .CountAsync(cancellationToken);

            if (failedAttemptsFromIp >= 5)
            {
                await LogSecurityEvent(request.TenantIdentifier, null, request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "IpAddressLockedOut", cancellationToken);
                return new LoginResponse(false, null, null, "Bu IP adresinden çok fazla hatalı giriş denemesi yapıldı. Lütfen 15 dakika sonra tekrar deneyin.");
            }
        }

        // 1. Kullanıcıyı getir (Kiracı ilişkileriyle birlikte)
        var user = await dbContext.Users
            .Include(u => u.TenantUsers)
            .ThenInclude(tu => tu.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant().Trim() && !u.IsDeleted, cancellationToken);

        // Güvenlik: Kullanıcı bulunamazsa zaman aşımı (Timing Attack) saldırılarını engellemek için 
        // hayali bir şifre hash doğrulaması koşturarak sunucu gecikmesini eşitliyoruz! 🛡️
        if (user == null)
        {
            passwordHasher.VerifyHashedPassword(new UserDummy(), "invalid_hash", request.Password);
            
            // Başarısızlığı logla
            await LogSecurityEvent(request.TenantIdentifier, null, request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "UserNotFound", cancellationToken);
            return new LoginResponse(false, null, null, genericErrorMessage);
        }

        // 1.5. Hesap Kilitli mi? (Lockout Policy)
        if (user.IsLockedOut)
        {
            var remainingMinutes = Math.Ceiling((user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes);
            await LogSecurityEvent(request.TenantIdentifier, user.Id.ToString(), request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "AccountLockedOut", cancellationToken);
            return new LoginResponse(false, null, null, $"Hesabınız çok fazla hatalı deneme nedeniyle kilitlenmiştir. {remainingMinutes} dakika sonra tekrar deneyin.");
        }

        // 2. Kullanıcı aktif mi?
        if (!user.IsActive)
        {
            await LogSecurityEvent(request.TenantIdentifier, user.Id.ToString(), request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "UserSuspended", cancellationToken);
            return new LoginResponse(false, null, null, "Hesabınız askıya alınmıştır.");
        }

        // 3. Şifre Doğrulaması
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
            user.IncrementAccessFailedCount();
            await dbContext.SaveChangesAsync(cancellationToken);

            await LogSecurityEvent(request.TenantIdentifier, user.Id.ToString(), request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "InvalidPassword", cancellationToken);
            return new LoginResponse(false, null, null, genericErrorMessage);
        }

        // Başarılı girişte kilit sayacını sıfırla
        user.ResetAccessFailedCount();

        // 4. Kiracı (Tenant) İlişkilerini Al
        var activeTenantUsers = user.TenantUsers.Where(tu => !tu.IsDeleted).ToList();
        if (activeTenantUsers.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await LogSecurityEvent(request.TenantIdentifier, user.Id.ToString(), request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "NoAssociatedTenants", cancellationToken);
            return new LoginResponse(false, null, null, "Hesabınız herhangi bir şirkete/kiracıya bağlı bulunmamaktadır.");
        }

        // 5. Kiracı Çözümlemesi (Tenant Resolution)
        
        // A. Kullanıcı doğrudan hedef bir kiracı belirterek girmeye çalışıyorsa:
        if (!string.IsNullOrWhiteSpace(request.TenantIdentifier))
        {
            var targetIdentifier = request.TenantIdentifier.ToLowerInvariant().Trim();
            var tenantUser = activeTenantUsers.FirstOrDefault(tu => tu.Tenant.Identifier == targetIdentifier);

            if (tenantUser == null)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await LogSecurityEvent(request.TenantIdentifier, user.Id.ToString(), request.Email, "LoginFailed", request.IpAddress, request.UserAgent, false, "TenantAccessDenied", cancellationToken);
                return new LoginResponse(false, null, null, "Bu şirket/kiracı ortamına erişim yetkiniz bulunmamaktadır.");
            }

            // JWT oluştur
            var token = tokenService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                tenantUser.TenantId,
                tenantUser.Tenant.Identifier,
                new[] { tenantUser.Role }
            );

            // Refresh Token oluştur
            var refreshTokenValue = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, DateTime.UtcNow.AddDays(7), request.IpAddress);
            dbContext.RefreshTokens.Add(refreshToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await LogSecurityEvent(tenantUser.Tenant.Identifier, user.Id.ToString(), request.Email, "LoginSuccess", request.IpAddress, request.UserAgent, true, null, cancellationToken);
            
            return new LoginResponse(true, token, refreshTokenValue, "Giriş başarılı.");
        }

        // B. Kullanıcı kiracı belirtmemişse:
        
        // Tek bir kiracıya üye ise doğrudan oraya login et:
        if (activeTenantUsers.Count == 1)
        {
            var tenantUser = activeTenantUsers[0];
            var token = tokenService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                tenantUser.TenantId,
                tenantUser.Tenant.Identifier,
                new[] { tenantUser.Role }
            );

            // Refresh Token oluştur
            var refreshTokenValue = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, DateTime.UtcNow.AddDays(7), request.IpAddress);
            dbContext.RefreshTokens.Add(refreshToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await LogSecurityEvent(tenantUser.Tenant.Identifier, user.Id.ToString(), request.Email, "LoginSuccess", request.IpAddress, request.UserAgent, true, null, cancellationToken);
            
            return new LoginResponse(true, token, refreshTokenValue, "Giriş başarılı.");
        }

        // Birden fazla kiracıya üye ise seçenek listesi dön:
        var tenantDtos = activeTenantUsers.Select(tu => new TenantDto(
            tu.TenantId, 
            tu.Tenant.Name, 
            tu.Tenant.Identifier
        ));

        await dbContext.SaveChangesAsync(cancellationToken);
        // Şirket seçimi aşamasına geçişi logla
        await LogSecurityEvent(null, user.Id.ToString(), request.Email, "LoginInitiatedWithMultipleTenants", request.IpAddress, request.UserAgent, true, null, cancellationToken);
        
        return new LoginResponse(
            IsSuccess: true, 
            Token: null, 
            RefreshToken: null,
            Message: "Lütfen giriş yapmak istediğiniz kiracıyı/şirketi seçiniz.", 
            AvailableTenants: tenantDtos
        );
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

// Timing attack önleyici sahte nesne (Şifre hasleyici imzasını korumak için)
internal class UserDummy : User
{
}
