using System;
using System.Linq;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Domain.Entities;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry;

/// <summary>
/// Telemetri sorguları üzerinde sürücü kısıtlamalarını (ABAC) tek noktadan yöneten
/// ve Clean Architecture prensiplerine uygun, tekrar kullanılabilir uzantı metotları! 🚛🛡️
/// </summary>
public static class TelemetryQueryExtensions
{
    public static IQueryable<Domain.Entities.Telemetry> ApplyDriverFilter(
        this IQueryable<Domain.Entities.Telemetry> query, 
        IUserTenantContext userTenantContext)
    {
        var isDriver = userTenantContext.Roles.Contains("Driver", StringComparer.OrdinalIgnoreCase) || 
                       userTenantContext.Roles.Contains("Sürücü", StringComparer.OrdinalIgnoreCase);

        if (isDriver)
        {
            if (Guid.TryParse(userTenantContext.UserId, out var driverUserId))
            {
                // Sürücü yalnızca kendisine zimmetli araca takılı cihazların telemetrilerini görebilir!
                return query.Where(t => t.Device.Vehicle != null && t.Device.Vehicle.DriverUserId == driverUserId);
            }
            
            // Güvenlik: Kimlik doğrulanamadıysa veri akışını tamamen kes!
            return query.Where(t => false);
        }

        return query;
    }
}
