using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

/// <summary>
/// IoT cihazlarından gelen telemetri verisini işleyen ve veri tabanına mühürleyen komut işleyicisi! 🌡️📡
/// Seri numarası üzerinden cihazı bulur, aktiflik durumunu denetler ve kiracı izolasyonuna uygun kaydeder.
/// </summary>
public class IngestTelemetryCommandHandler(IoTDbContext dbContext) 
    : IRequestHandler<IngestTelemetryCommand, IngestTelemetryResponse>
{
    public async Task<IngestTelemetryResponse> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
    {
        var serialNumber = request.DeviceId.ToUpperInvariant().Trim();

        // 1. Cihazı seri numarasıyla tüm sistemde (kiracı kısıtlamasını aşarak) arıyoruz
        var device = await dbContext.Devices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            return new IngestTelemetryResponse(
                Message: $"Telemetri reddedildi: Seri numarası [{serialNumber}] olan cihaz sistemde kayıtlı değil!",
                IsSuccess: false,
                ProcessedAt: DateTime.UtcNow
            );
        }

        if (!device.IsActive)
        {
            return new IngestTelemetryResponse(
                Message: $"Telemetri reddedildi: Cihaz [{device.Name}] pasif durumda!",
                IsSuccess: false,
                ProcessedAt: DateTime.UtcNow
            );
        }

        // 2. Cihazın bağlı olduğu kiracıya (TenantId) göre telemetriyi mühürlüyoruz
        var telemetry = Domain.Entities.Telemetry.Create(
            device.TenantId,
            device.Id,
            request.Temperature,
            request.Pressure,
            request.Timestamp
        );

        dbContext.Telemetries.Add(telemetry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IngestTelemetryResponse(
            Message: $"Telemetri başarıyla alındı: [{device.Name}] -> Sıcaklık: {request.Temperature}°C, Basınç: {request.Pressure} hPa. ⚡",
            IsSuccess: true,
            ProcessedAt: DateTime.UtcNow
        );
    }
}