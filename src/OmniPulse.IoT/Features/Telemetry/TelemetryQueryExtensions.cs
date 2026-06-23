using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.IoT.Domain.Entities;
using OmniPulse.IoT.Infrastructure.Persistence;
using System;
using System.Linq;

namespace OmniPulse.IoT.Features.Telemetry;

/// <summary>
/// Telemetri sorguları üzerinde varlık-tabanlı erişim kısıtlamalarını (ABAC) tek noktadan yöneten
/// ve Clean Architecture prensiplerine uygun, PostgreSQL Recursive CTE destekli uzantı metotları! 🏭🚛🛡️
/// </summary>
public static class TelemetryQueryExtensions
{
    public static IQueryable<Domain.Entities.Telemetry> ApplyAssetFilter(
        this IQueryable<Domain.Entities.Telemetry> query, 
        IUserTenantContext userTenantContext,
        IoTDbContext dbContext)
    {
        // Sürücü, saha çalışanı veya operatör yalnızca sorumlu olduğu varlığın telemetrilerini görebilir
        var isFieldUser = 
            userTenantContext.Roles.Contains("Driver", StringComparer.OrdinalIgnoreCase) || 
            userTenantContext.Roles.Contains("Sürücü", StringComparer.OrdinalIgnoreCase) ||
            userTenantContext.Roles.Contains("FieldWorker", StringComparer.OrdinalIgnoreCase) ||
            userTenantContext.Roles.Contains("Operator", StringComparer.OrdinalIgnoreCase);

        if (isFieldUser)
        {
            if (Guid.TryParse(userTenantContext.UserId, out var userId))
            {
                // PostgreSQL'de Recursive CTE kullanarak kullanıcının yetkili olduğu tüm varlıkları (alt dallar dahil) bulur.
                // Hem yeni AssetPermissions tablosunu hem de eski ResponsibleUserId kolonunu destekler.
                var allowedAssets = dbContext.Assets.FromSqlRaw(@"
                    WITH RECURSIVE AllowedAssets AS (
                        -- 1. AssetPermissions tablosundan kullanıcının rolleri
                        SELECT ap.""AssetId"" AS ""Id""
                        FROM ""AssetPermissions"" ap
                        WHERE ap.""UserId"" = {0} AND ap.""IsDeleted"" = false
                        
                        UNION
                        
                        -- 2. Eski Asset.ResponsibleUserId alanından geri dönük uyumluluk
                        SELECT a.""Id""
                        FROM ""Assets"" a
                        WHERE a.""ResponsibleUserId"" = {0} AND a.""IsDeleted"" = false
                        
                        UNION
                        
                        -- 3. Hiyerarşik olarak alt varlıkları (çocuk dalları) bul
                        SELECT child.""Id""
                        FROM ""Assets"" child
                        INNER JOIN AllowedAssets parent ON child.""ParentAssetId"" = parent.""Id""
                        WHERE child.""IsDeleted"" = false
                    )
                    SELECT * FROM ""Assets"" WHERE ""Id"" IN (SELECT ""Id"" FROM AllowedAssets)
                ", userId);

                // Telemetriyi, cihazın bağlı olduğu varlık bu izinli varlıklar listesindeyse getir
                return query.Where(t => t.Device.AssetId.HasValue && allowedAssets.Any(a => a.Id == t.Device.AssetId.Value));
            }
            
            // Güvenlik: Kimlik doğrulanamadıysa veri akışını tamamen kes!
            return query.Where(t => false);
        }

        return query;
    }
}
