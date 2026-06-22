using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Devices.CreateDevice;

public class CreateDeviceCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<CreateDeviceCommand, CreateDeviceResponse>
{
    public async Task<CreateDeviceResponse> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını doğrula
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Yetkilendirme Kontrolü (Delege Etme: Sadece Owner, Admin veya IoT_Admin yeni cihaz yaratabilir)
        var allowedRoles = new[] { "Owner", "Admin", "IoT_Admin" };
        var hasAccess = userTenantContext.Roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu kiracı üzerinde yeni bir cihaz (sensör) envanter kaydı oluşturma yetkiniz bulunmamaktadır! Lütfen yöneticinizle irtibata geçin. 🔐");
        }

        // 3. Benzersiz Seri Numarası Kontrolü
        var serialNormalized = request.SerialNumber.ToUpperInvariant().Trim();
        var serialExists = await dbContext.Devices
            .IgnoreQueryFilters() // Silinmişler dahil tüm envanteri kontrol et çakışmayı önlemek için
            .AnyAsync(d => d.SerialNumber == serialNormalized, cancellationToken);

        if (serialExists)
        {
            throw new ArgumentException($"[{serialNormalized}] seri numaralı cihaz zaten envantere kayıtlı!");
        }

        // 4. Kategori Doğrulaması (opsiyonel belirtildiyse)
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await dbContext.DeviceCategories
                .AnyAsync(c => c.Id == request.CategoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                throw new KeyNotFoundException($"Belirtilen cihaz kategorisi [{request.CategoryId.Value}] bulunamadı!");
            }
        }

        // 5. Varlık Doğrulaması (opsiyonel doğrudan montajlı eklendiyse)
        if (request.AssetId.HasValue)
        {
            var assetExists = await dbContext.Assets
                .AnyAsync(a => a.Id == request.AssetId.Value, cancellationToken);

            if (!assetExists)
            {
                throw new KeyNotFoundException($"Belirtilen varlık [{request.AssetId.Value}] bulunamadı!");
            }
        }

        // 6. Cihazı oluştur ve kaydet
        var device = Device.Create(
            tenantId,
            request.Name,
            request.SerialNumber,
            request.AssetId,
            request.CategoryId
        );

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateDeviceResponse(
            Id: device.Id,
            Name: device.Name,
            SerialNumber: device.SerialNumber,
            CategoryId: device.CategoryId,
            AssetId: device.AssetId,
            Message: $"[{device.Name}] cihazı [{device.SerialNumber}] seri numarasıyla sisteme başarıyla kaydedildi! 🔌"
        );
    }
}
