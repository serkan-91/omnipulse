using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Devices.CleanupDemoData;

public class CleanupDemoDataCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CleanupDemoDataCommand, CleanupDemoDataResponse>
{
    public async Task<CleanupDemoDataResponse> Handle(CleanupDemoDataCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını doğrula
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Yetkilendirme Kontrolü (Sadece Owner veya Admin temizlik yapabilir)
        var allowedRoles = new[] { "Owner", "Admin" };
        var hasAccess = userTenantContext.Roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu kiracı üzerinde veri sıfırlama/temizleme yetkiniz bulunmamaktadır! 🔐");
        }

        // 3. Transaction Başlatıyoruz
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Kiracının tüm verilerini çekiyoruz (EF Core global query filter zaten tenantId uygulayacaktır ama biz yine de emin olalım)
            
            // A. Telemetri verilerini temizle (Hard Delete)
            var telemetries = await dbContext.Telemetries
                .Where(t => t.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            
            if (telemetries.Any())
            {
                dbContext.Telemetries.RemoveRange(telemetries);
            }

            // B. Cihazları temizle (Hard Delete - Seri numaraları tekrar kullanılabilmesi için demo temizliğinde hard delete yapıyoruz)
            var devices = await dbContext.Devices
                .IgnoreQueryFilters() // Silinmişler dahil her şeyi temizle
                .Where(d => d.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            
            if (devices.Any())
            {
                dbContext.Devices.RemoveRange(devices);
            }

            // C. Varlıkları (Assets) temizle (Hiyerarşiden dolayı alt varlıklar ve kök varlıkları siliyoruz)
            var assets = await dbContext.Assets
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            
            if (assets.Any())
            {
                dbContext.Assets.RemoveRange(assets);
            }

            // D. Kategorileri temizle
            var categories = await dbContext.DeviceCategories
                .IgnoreQueryFilters()
                .Where(c => c.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            
            if (categories.Any())
            {
                dbContext.DeviceCategories.RemoveRange(categories);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CleanupDemoDataResponse(
                IsSuccess: true,
                Message: "Tüm demo ve test cihazı verileriniz, varlıklarınız ve telemetri geçmişiniz başarıyla temizlendi! Envanteriniz sıfırlandı. 🧼✨"
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new CleanupDemoDataResponse(
                IsSuccess: false,
                Message: $"Veriler temizlenirken hata oluştu: {ex.Message}"
            );
        }
    }
}
