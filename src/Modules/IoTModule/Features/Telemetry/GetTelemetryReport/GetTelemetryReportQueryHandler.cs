using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.GetTelemetryReport;

public class GetTelemetryReportQueryHandler(IoTDbContext dbContext)
    : IRequestHandler<GetTelemetryReportQuery, TelemetryReportDto>
{
    public async Task<TelemetryReportDto> Handle(GetTelemetryReportQuery request, CancellationToken cancellationToken)
    {
        var isTemp = request.MetricKey.Equals("Temperature", StringComparison.OrdinalIgnoreCase);

        var query = dbContext.Telemetries
            .Include(t => t.Device)
            .ThenInclude(d => d.Vehicle)
            .Where(t => t.Timestamp >= request.StartDate && t.Timestamp <= request.EndDate);

        if (request.DeviceId.HasValue)
        {
            query = query.Where(t => t.DeviceId == request.DeviceId.Value);
        }

        if (request.VehicleId.HasValue)
        {
            query = query.Where(t => t.Device.VehicleId == request.VehicleId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new TelemetryReportDto(
                MinValue: 0,
                MaxValue: 0,
                AverageValue: 0,
                TotalCount: 0,
                BreachCount: 0,
                BreachPercentage: 0,
                BreachedPoints: new List<BreachPointDto>(),
                Message: $"Belirtilen zaman aralığında ({request.StartDate:dd/MM/yyyy HH:mm} - {request.EndDate:dd/MM/yyyy HH:mm}) herhangi bir telemetri verisi bulunamadı."
            );
        }

        // Summary metrics using SQL aggregations to conserve memory and network bandwidth! 🚀
        double min, max, avg;
        if (isTemp)
        {
            min = await query.MinAsync(t => t.Temperature, cancellationToken);
            max = await query.MaxAsync(t => t.Temperature, cancellationToken);
            avg = await query.AverageAsync(t => t.Temperature, cancellationToken);
        }
        else
        {
            min = await query.MinAsync(t => t.Pressure, cancellationToken);
            max = await query.MaxAsync(t => t.Pressure, cancellationToken);
            avg = await query.AverageAsync(t => t.Pressure, cancellationToken);
        }

        // Cold chain breach filtering
        var threshold = request.ColdChainThreshold;
        var breachQuery = query;
        if (isTemp)
        {
            breachQuery = breachQuery.Where(t => t.Temperature > threshold);
        }
        else
        {
            breachQuery = breachQuery.Where(t => t.Pressure > threshold);
        }

        var breachedPoints = await breachQuery
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new BreachPointDto(
                t.Id,
                t.DeviceId,
                t.Device.Name,
                t.Device.Vehicle != null ? t.Device.Vehicle.PlateNumber : null,
                isTemp ? t.Temperature : t.Pressure,
                threshold,
                t.Timestamp
            ))
            .ToListAsync(cancellationToken);

        var breachPercentage = (double)breachedPoints.Count / totalCount * 100.0;
        
        var msg = breachedPoints.Count > 0
            ? $"ANALİZ TAMAMLANDI: Belirtilen zaman aralığında toplam {totalCount} okumadan {breachedPoints.Count} adedinde soğuk zincir kırılması (eşik ihlali) tespit edilmiştir! ⚠️"
            : "ANALİZ TAMAMLANDI: Harika! Soğuk zincirde herhangi bir kırılma tespit edilmemiştir. ✅";

        return new TelemetryReportDto(
            MinValue: Math.Round(min, 2),
            MaxValue: Math.Round(max, 2),
            AverageValue: Math.Round(avg, 2),
            TotalCount: totalCount,
            BreachCount: breachedPoints.Count,
            BreachPercentage: Math.Round(breachPercentage, 2),
            BreachedPoints: breachedPoints,
            Message: msg
        );
    }
}
