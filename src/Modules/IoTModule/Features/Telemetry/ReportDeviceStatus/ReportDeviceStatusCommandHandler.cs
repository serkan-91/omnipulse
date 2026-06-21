using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.ReportDeviceStatus;

public class ReportDeviceStatusCommandHandler(
    IoTDbContext dbContext,
    ILogger<ReportDeviceStatusCommandHandler> logger)
    : IRequestHandler<ReportDeviceStatusCommand, ReportDeviceStatusResponse>
{
    public async Task<ReportDeviceStatusResponse> Handle(ReportDeviceStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var serialNumber = request.DeviceId.ToUpperInvariant().Trim();
            var currentTraceId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();

            // 1. Cihazı seri numarasıyla veritabanında arıyoruz
            var device = await dbContext.Devices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber && !d.IsDeleted, cancellationToken);

            if (device == null)
            {
                logger.LogWarning("⚠️ Bilinmeyen cihaz seri numarasıyla bağlantı durumu bildirme denemesi: {Serial}", serialNumber);

                // Yetkisiz erişim/deneme güvenlik olayı Outbox'a yazılıyor 🚨
                var outboxEvent = OutboxEvent.Create(
                    eventType: "SecurityAudit",
                    payload: JsonSerializer.Serialize(new
                    {
                        EventType = "SecurityAudit",
                        Action = "UnknownDeviceConnectionAttempt",
                        DeviceSerialNumber = serialNumber,
                        Message = $"Sistemde kayıtlı olmayan bir cihaz bağlantı kurmaya çalıştı: {serialNumber}",
                        Timestamp = DateTime.UtcNow,
                        TraceId = currentTraceId
                    }),
                    partitionKey: serialNumber
                );

                dbContext.OutboxEvents.Add(outboxEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new ReportDeviceStatusResponse(
                    Message: $"Durum bildirimi reddedildi: Cihaz [{serialNumber}] sistemde kayıtlı değil!",
                    IsSuccess: false,
                    ProcessedAt: DateTime.UtcNow
                );
            }

            // 2. Cihaz aktif mi? Pasif durumdaysa bağlantı durumunu da reddedebiliriz (güvenlik kalkanı)
            if (!device.IsActive)
            {
                logger.LogWarning("⚠️ Pasif durumdaki cihazdan bağlantı durumu bildirimi reddedildi: {Serial}", serialNumber);

                var outboxEvent = OutboxEvent.Create(
                    eventType: "SecurityAudit",
                    payload: JsonSerializer.Serialize(new
                    {
                        EventType = "SecurityAudit",
                        Action = "InactiveDeviceConnectionAttempt",
                        DeviceSerialNumber = device.SerialNumber,
                        TenantId = device.TenantId,
                        Message = $"Pasif/askıya alınmış bir cihaz bağlantı kurmaya çalıştı: {device.Name} ({device.SerialNumber})",
                        Timestamp = DateTime.UtcNow,
                        TraceId = currentTraceId
                    }),
                    partitionKey: device.SerialNumber
                );

                dbContext.OutboxEvents.Add(outboxEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new ReportDeviceStatusResponse(
                    Message: $"Durum bildirimi reddedildi: Cihaz [{device.Name}] pasif durumda!",
                    IsSuccess: false,
                    ProcessedAt: DateTime.UtcNow
                );
            }

            // 3. Bağlantı/Durum Değişikliği Olayını Outbox'a Aktar 🔌
            var statusOutboxEvent = OutboxEvent.Create(
                eventType: "DeviceConnection",
                payload: JsonSerializer.Serialize(new
                {
                    EventType = "DeviceConnection",
                    DeviceSerialNumber = device.SerialNumber,
                    TenantId = device.TenantId,
                    IsOnline = request.IsOnline,
                    Message = $"Cihaz bağlantı durumu değişti: {device.Name} -> {(request.IsOnline ? "ONLINE" : "OFFLINE")}",
                    Timestamp = request.Timestamp,
                    TraceId = currentTraceId
                }),
                partitionKey: device.SerialNumber
            );

            dbContext.OutboxEvents.Add(statusOutboxEvent);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cihaz bağlantı durumu Outbox'a aktarıldı: {Serial} -> {Status}", 
                device.SerialNumber, request.IsOnline ? "ONLINE" : "OFFLINE");

            return new ReportDeviceStatusResponse(
                Message: $"Cihaz durumu başarıyla güncellendi ve Outbox kuyruğuna kaydedildi: {device.Name} -> {(request.IsOnline ? "ONLINE" : "OFFLINE")}",
                IsSuccess: true,
                ProcessedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cihaz bağlantı durumu işlenirken beklenmeyen sistem hatası! Cihaz: {Serial}", request.DeviceId);
            return new ReportDeviceStatusResponse(
                Message: "Sistem hatası oluştu.",
                IsSuccess: false,
                ProcessedAt: DateTime.UtcNow
            );
        }
    }
}
