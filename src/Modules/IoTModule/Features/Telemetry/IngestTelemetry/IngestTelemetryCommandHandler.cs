using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;
using OmniPulse.Modules.IoTModule.Infrastructure.Streaming;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

/// <summary>
/// IoT cihazlarından gelen telemetri verisini işleyen, veri tabanına mühürleyen
/// ve yüksek ölçekli analiz için AWS Kinesis'e pompalayan komut işleyicisi! 🌡️📡
/// </summary>
public class IngestTelemetryCommandHandler(
    IoTDbContext dbContext,
    MediatR.IMediator mediator,
    IKinesisTelemetryPublisher kinesisPublisher,
    ILogger<IngestTelemetryCommandHandler> logger) 
    : IRequestHandler<IngestTelemetryCommand, IngestTelemetryResponse>
{
    public async Task<IngestTelemetryResponse> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var serialNumber = request.DeviceId.ToUpperInvariant().Trim();

            // 1. Cihazı seri numarasıyla tüm sistemde (kiracı kısıtlamasını aşarak) arıyoruz
            var device = await dbContext.Devices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber && !d.IsDeleted, cancellationToken);

            if (device == null)
            {
                logger.LogWarning("Bilinmeyen cihaz seri numarasıyla telemetri denemesi: {Serial}", serialNumber);
                
                // Güvenlik audit olayını Kinesis'e aktarıyoruz 🚨
                await kinesisPublisher.PublishAsync(
                    partitionKey: serialNumber,
                    telemetryData: new
                    {
                        EventType = "SecurityAudit",
                        Action = "UnknownDeviceAttempt",
                        DeviceSerialNumber = serialNumber,
                        Message = $"Sistemde kayıtlı olmayan cihazdan telemetri gönderilmeye çalışıldı: {serialNumber}",
                        Timestamp = DateTime.UtcNow
                    },
                    cancellationToken
                );

                return new IngestTelemetryResponse(
                    Message: $"Telemetri reddedildi: Seri numarası [{serialNumber}] olan cihaz sistemde kayıtlı değil!",
                    IsSuccess: false,
                    ProcessedAt: DateTime.UtcNow
                );
            }

            if (!device.IsActive)
            {
                logger.LogWarning("Pasif durumdaki cihazdan telemetri reddedildi: {Serial}", serialNumber);

                // Güvenlik audit olayını Kinesis'e aktarıyoruz 🚨
                await kinesisPublisher.PublishAsync(
                    partitionKey: device.SerialNumber,
                    telemetryData: new
                    {
                        EventType = "SecurityAudit",
                        Action = "InactiveDeviceAttempt",
                        DeviceSerialNumber = device.SerialNumber,
                        TenantId = device.TenantId,
                        Message = $"Pasif/engellenmiş cihazdan veri akışı denendi: {device.Name} ({device.SerialNumber})",
                        Timestamp = DateTime.UtcNow
                    },
                    cancellationToken
                );

                return new IngestTelemetryResponse(
                    Message: $"Telemetri reddedildi: Cihaz [{device.Name}] pasif durumda!",
                    IsSuccess: false,
                    ProcessedAt: DateTime.UtcNow
                );
            }

            // 2. Cihazın bağlı olduğu kiracıya (TenantId) göre telemetriyi mühürlüyoruz (PostgreSQL)
            var telemetry = Domain.Entities.Telemetry.Create(
                device.TenantId,
                device.Id,
                request.Temperature,
                request.Pressure,
                request.Timestamp
            );

            dbContext.Telemetries.Add(telemetry);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 3. AWS Kinesis'e Yüksek Performanslı Asenkron Yayın (Real-Time Stream Pipeline)
            // PartitionKey olarak cihazın seri numarasını veriyoruz; böylece aynı cihazın verileri 
            // Kinesis shard'larında her zaman ardışık ve sıralı işlenir! 🚀
            await kinesisPublisher.PublishAsync(
                partitionKey: device.SerialNumber,
                telemetryData: new
                {
                    TelemetryId = telemetry.Id,
                    DeviceSerialNumber = device.SerialNumber,
                    TenantId = device.TenantId,
                    telemetry.Temperature,
                    telemetry.Pressure,
                    telemetry.Timestamp
                },
                cancellationToken
            );

            // 4. Alarmların tetiklenmesi için olayı (event) yayınla 🔔
            await mediator.Publish(new TelemetryIngestedEvent(
                device.TenantId,
                device.Id,
                device.Name,
                request.Temperature,
                request.Pressure,
                request.Timestamp
            ), cancellationToken);

            return new IngestTelemetryResponse(
                Message: $"Telemetri başarıyla alındı ve Kinesis akışına pompalandı! ⚡ Cihaz: [{device.Name}] -> Sıcaklık: {request.Temperature}°C, Basınç: {request.Pressure} hPa.",
                IsSuccess: true,
                ProcessedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Telemetri işlenirken beklenmeyen sistem hatası! Cihaz: {Serial}", request.DeviceId);
            return new IngestTelemetryResponse(
                Message: "Sistem hatası oluştu.",
                IsSuccess: false,
                ProcessedAt: DateTime.UtcNow
            );
        }
    }
}