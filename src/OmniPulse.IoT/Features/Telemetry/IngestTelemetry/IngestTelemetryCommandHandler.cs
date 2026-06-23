using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniPulse.IoT.Domain.Entities;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Features.Telemetry.IngestTelemetry;

/// <summary>
/// IoT cihazlarından gelen telemetri verisini işleyen, veri tabanına mühürleyen
/// ve transactional outbox tablosuna yazan komut işleyicisi! 🌡️📡
/// </summary>
public class IngestTelemetryCommandHandler(
    IoTDbContext dbContext,
    MediatR.IMediator mediator,
    ILogger<IngestTelemetryCommandHandler> logger) 
    : IRequestHandler<IngestTelemetryCommand, IngestTelemetryResponse>
{
    public async Task<IngestTelemetryResponse> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var serialNumber = request.DeviceId.ToUpperInvariant().Trim();
            var currentTraceId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();

            // 1. Cihazı seri numarasıyla tüm sistemde (kiracı kısıtlamasını aşarak) arıyoruz
            var device = await dbContext.Devices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber && !d.IsDeleted, cancellationToken);

            if (device == null)
            {
                logger.LogWarning("Bilinmeyen cihaz seri numarasıyla telemetri denemesi: {Serial}", serialNumber);
                
                // Güvenlik audit olayını transactional outbox'a yazıyoruz 🚨
                var outboxEvent = OutboxEvent.Create(
                    eventType: "SecurityAudit",
                    payload: JsonSerializer.Serialize(new
                    {
                        EventType = "SecurityAudit",
                        Action = "UnknownDeviceAttempt",
                        DeviceSerialNumber = serialNumber,
                        Message = $"Sistemde kayıtlı olmayan cihazdan telemetri gönderilmeye çalışıldı: {serialNumber}",
                        Timestamp = DateTime.UtcNow,
                        TraceId = currentTraceId
                    }),
                    partitionKey: serialNumber
                );

                dbContext.OutboxEvents.Add(outboxEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new IngestTelemetryResponse(
                    Message: $"Telemetri reddedildi: Seri numarası [{serialNumber}] olan cihaz sistemde kayıtlı değil!",
                    IsSuccess: false,
                    ProcessedAt: DateTime.UtcNow
                );
            }

            if (!device.IsActive)
            {
                logger.LogWarning("Pasif durumdaki cihazdan telemetri reddedildi: {Serial}", serialNumber);

                // Güvenlik audit olayını transactional outbox'a yazıyoruz 🚨
                var outboxEvent = OutboxEvent.Create(
                    eventType: "SecurityAudit",
                    payload: JsonSerializer.Serialize(new
                    {
                        EventType = "SecurityAudit",
                        Action = "InactiveDeviceAttempt",
                        DeviceSerialNumber = device.SerialNumber,
                        TenantId = device.TenantId,
                        Message = $"Pasif/engellenmiş cihazdan veri akışı denendi: {device.Name} ({device.SerialNumber})",
                        Timestamp = DateTime.UtcNow,
                        TraceId = currentTraceId
                    }),
                    partitionKey: device.SerialNumber
                );

                dbContext.OutboxEvents.Add(outboxEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

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

            // 3. AWS Kinesis'e iletilecek olay için OutboxEvent oluşturup aynı transaction'a ekliyoruz 📦
            var telemetryOutboxEvent = OutboxEvent.Create(
                eventType: "TelemetryIngested",
                payload: JsonSerializer.Serialize(new
                {
                    TelemetryId = telemetry.Id,
                    DeviceSerialNumber = device.SerialNumber,
                    TenantId = device.TenantId,
                    telemetry.Temperature,
                    telemetry.Pressure,
                    telemetry.Timestamp,
                    TraceId = currentTraceId
                }),
                partitionKey: device.SerialNumber
            );

            dbContext.OutboxEvents.Add(telemetryOutboxEvent);

            // SaveChangesAsync hem telemetriyi hem outboxEvent'i tek bir transaction'da atomik yazar! 🛡️
            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. Alarmların tetiklenmesi için olayı (event) yayınla 🔔
            await mediator.Publish(new TelemetryIngestedEvent(
                device.TenantId,
                device.Id,
                device.Name,
                request.Temperature,
                request.Pressure,
                request.Timestamp,
                TraceId: currentTraceId
            ), cancellationToken);

            return new IngestTelemetryResponse(
                Message: $"Telemetri başarıyla alındı ve Outbox kuyruğuna kaydedildi! ⚡ Cihaz: [{device.Name}] -> Sıcaklık: {request.Temperature}°C, Basınç: {request.Pressure} hPa.",
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