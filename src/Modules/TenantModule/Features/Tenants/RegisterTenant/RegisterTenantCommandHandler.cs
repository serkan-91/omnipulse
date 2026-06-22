using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.TenantModule.Features.Tenants.RegisterTenant;

public class RegisterTenantCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResponse>
{
    public async Task<RegisterTenantResponse> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var normalizedIdentifier = request.TenantIdentifier.ToLowerInvariant().Trim();

        // 1. Güvenlik ve Benzersizlik Kontrolleri
        if (await dbContext.Tenants.AnyAsync(t => t.Identifier == normalizedIdentifier && !t.IsDeleted, cancellationToken))
        {
            return new RegisterTenantResponse(false, null, $"'{normalizedIdentifier}' şirket kısa adı zaten kullanımda. Başka bir tane seçelim.");
        }

        if (await dbContext.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant().Trim() && !u.IsDeleted, cancellationToken))
        {
            return new RegisterTenantResponse(false, null, $"'{request.Email}' e-posta adresi ile zaten bir hesap mevcut.");
        }

        // 2. Transaction Başlatıyoruz (ACID güvencesi)
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 3. Yeni Tenant (Kiracı) Nesnesini Oluşturma
            // Varsayılan olarak paylaşılan DB kullanılacağı için ConnectionString null/boş bırakılıyor.
            var tenant = Tenant.Create(
                request.CompanyName,
                normalizedIdentifier,
                null,
                DateTime.UtcNow.AddYears(1)
            );

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken); // Tenant ID üretilmesi ve audit için kaydediyoruz

            // 4. Yeni Kullanıcıyı (Owner) Oluşturma
            var dummyUser = User.Create(request.FirstName, request.LastName, request.Email, "temporary");
            var hashedPassword = passwordHasher.HashPassword(dummyUser, request.Password);
            
            var user = User.Create(
                request.FirstName,
                request.LastName,
                request.Email,
                hashedPassword
            );

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 5. Ara Tablo (TenantUser) İlişkisini "Owner" Rolüyle Kurma
            var tenantUser = TenantUser.Create(tenant.Id, user.Id, "Owner");
            dbContext.TenantUsers.Add(tenantUser);

            await dbContext.SaveChangesAsync(cancellationToken);

            // Transaction'ı başarıyla sonlandırıyoruz
            await transaction.CommitAsync(cancellationToken);

            // 6. Kullanıcıyı anında içeri almak için bir JWT Token üretiyoruz
            var token = tokenService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                tenant.Id,
                tenant.Identifier,
                new[] { "Owner" }
            );

            return new RegisterTenantResponse(true, token, "Kayıt işlemi başarıyla tamamlandı.");
        }
        catch (Exception ex)
        {
            // Hata olursa her şeyi geri alıyoruz
            await transaction.RollbackAsync(cancellationToken);
            return new RegisterTenantResponse(false, null, $"Self-service kayıt işlemi sırasında hata oluştu: {ex.Message}");
        }
    }
}
