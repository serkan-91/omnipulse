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

namespace OmniPulse.Modules.IoTModule.Features.Devices.SeedDemoData;

public class SeedDemoDataCommandHandler(
    IoTDbContext dbContext,
    IUserTenantContext userTenantContext)
    : IRequestHandler<SeedDemoDataCommand, SeedDemoDataResponse>
{
    public async Task<SeedDemoDataResponse> Handle(SeedDemoDataCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant/Kiracı bağlamını doğrula
        if (!userTenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Aktif bir kiracı bağlamı bulunamadı şefim! 🔐");
        }

        var tenantId = userTenantContext.TenantId.Value;

        // 2. Yetkilendirme Kontrolü (Sadece Owner, Admin veya IoT_Admin demo veri yükleyebilir)
        var allowedRoles = new[] { "Owner", "Admin", "IoT_Admin" };
        var hasAccess = userTenantContext.Roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Bu kiracı üzerinde demo verisi yükleme yetkiniz bulunmamaktadır! 🔐");
        }

        // 3. Mevcut cihaz kontrolü (Eğer zaten cihaz varsa temiz veri setini bozmamak adına izin vermiyoruz)
        var hasExistingDevices = await dbContext.Devices.AnyAsync(cancellationToken);
        if (hasExistingDevices)
        {
            return new SeedDemoDataResponse(
                IsSuccess: false,
                Message: "Kiracınızda zaten kayıtlı cihazlar bulunuyor! Demo verisi yüklemek için envanterinizin boş olması gerekir.",
                SectorsCount: 0,
                AssetsCount: 0,
                DevicesCount: 0
            );
        }

        // 4. Transaction Başlatıyoruz
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 5. Cihaz Kategorilerini Ekle
            var tempCategory = DeviceCategory.Create(tenantId, "Sıcaklık Sensörleri", "Ortam ve soğuk hava tankı sıcaklık/nem takibi yapan IoT sensörleri.");
            var vibCategory = DeviceCategory.Create(tenantId, "Titreşim Sensörleri", "Endüstriyel motor ve rulmanların titreşim/frekans analizi sensörleri.");
            var pressCategory = DeviceCategory.Create(tenantId, "Basınç Sensörleri", "Boru hatları ve pnömatik kompresör basıncı ölçen sensörler.");

            dbContext.DeviceCategories.AddRange(tempCategory, vibCategory, pressCategory);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 6. Varlıkları (Assets) Ekle (Ağaç Hiyerarşisiyle)
            // Lojistik Kolu
            var logisticsDept = Asset.Create(tenantId, "Lojistik Departmanı", "Logistics", metadataJson: "{\"region\":\"Marmara\",\"manager\":\"Ayşe Şen\"}");
            dbContext.Assets.Add(logisticsDept);
            await dbContext.SaveChangesAsync(cancellationToken);

            var refrigeratedTruck = Asset.Create(tenantId, "Soğutmalı Tır-07", "Logistics", parentAssetId: logisticsDept.Id, metadataJson: "{\"plateNumber\":\"34ABC123\",\"capacity\":\"20T\",\"coolantType\":\"R404A\"}");
            dbContext.Assets.Add(refrigeratedTruck);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Akıllı Fabrika Kolu
            var smartFactory = Asset.Create(tenantId, "Akıllı Fabrika", "Factory", metadataJson: "{\"address\":\"Kocaeli OSB No:12\",\"capacity\":\"1000u/hr\"}");
            dbContext.Assets.Add(smartFactory);
            await dbContext.SaveChangesAsync(cancellationToken);

            var conveyorBelt = Asset.Create(tenantId, "Konveyör Bant-A", "Factory", parentAssetId: smartFactory.Id, metadataJson: "{\"rpmLimit\":1500,\"criticalTemp\":65.0}");
            dbContext.Assets.Add(conveyorBelt);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 7. Cihazları Ekle (Sensör Donanımları)
            var suffix = tenantId.ToString().Substring(0, 8).ToUpperInvariant();
            
            var tempSensor = Device.Create(tenantId, "Kasa Sıcaklık Sensörü-A", $"SN-TEMP-{suffix}", assetId: refrigeratedTruck.Id, categoryId: tempCategory.Id);
            var vibSensor = Device.Create(tenantId, "Bant Titreşim Sensörü-X", $"SN-VIB-{suffix}", assetId: conveyorBelt.Id, categoryId: vibCategory.Id);
            var pressSensor = Device.Create(tenantId, "Bant Basınç Sensörü-Y", $"SN-PRES-{suffix}", assetId: conveyorBelt.Id, categoryId: pressCategory.Id);

            dbContext.Devices.AddRange(tempSensor, vibSensor, pressSensor);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 8. Geçmiş Telemetri Verilerini Oluştur (Son 2 saat, 10'ar dakika arayla)
            var telemetries = new List<Domain.Entities.Telemetry>();
            var now = DateTime.UtcNow;
            var random = new Random();

            for (int i = 12; i >= 0; i--)
            {
                var timestamp = now.AddMinutes(-i * 10);

                // Sıcaklık Sensörü: 4.0°C ile 5.5°C arası (Normal)
                var tempVal = 4.0 + (random.NextDouble() * 1.5);
                var pressVal1 = 101.3 + (random.NextDouble() * 0.5); // Standart atm basıncı
                telemetries.Add(Domain.Entities.Telemetry.Create(tenantId, tempSensor.Id, tempVal, pressVal1, timestamp));

                // Titreşim Sensörü: 35.0°C - 45.0°C sıcaklık, 70-90 Hz titreşim (basınç olarak simüle edildi)
                var vibTemp = 35.0 + (random.NextDouble() * 10.0);
                var vibFreq = 70.0 + (random.NextDouble() * 20.0);
                telemetries.Add(Domain.Entities.Telemetry.Create(tenantId, vibSensor.Id, vibTemp, vibFreq, timestamp));

                // Basınç Sensörü: 25.0°C civarı, 120-140 kPa basınç
                var pressTemp = 24.5 + (random.NextDouble() * 1.0);
                var pressVal = 120.0 + (random.NextDouble() * 20.0);
                telemetries.Add(Domain.Entities.Telemetry.Create(tenantId, pressSensor.Id, pressTemp, pressVal, timestamp));
            }

            dbContext.Telemetries.AddRange(telemetries);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Her şey tamamsa transaction'ı tamamla!
            await transaction.CommitAsync(cancellationToken);

            return new SeedDemoDataResponse(
                IsSuccess: true,
                Message: "Tebrikler! Kiracınız için 3 Cihaz Kategorisi, 4 Varlık (2 Kök, 2 Alt Varlık), 3 Aktif Cihaz ve 2 saatlik geçmiş canlı telemetri verileri başarıyla yüklendi. 🚀📈",
                SectorsCount: 2, // Logistics & Factory
                AssetsCount: 4,
                DevicesCount: 3
            );
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
