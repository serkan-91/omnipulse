using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;
using OmniPulse.Modules.IoTModule.Infrastructure.Streaming;

namespace OmniPulse.Modules.IoTModule.Features.Telemetry.ReportDeviceStatus;

public class ReportDeviceStatusCommandHandler(
    IoTDbContext dbContext,
    IKinesisTelemetryPublisher kinesisPublisher,
    ILogger<ReportDeviceStatusCommandHandler> logger)
    : IRequestHandler<ReportDeviceStatusCommand, ReportDeviceStatusResponse>
{
    public async Task<ReportDeviceStatusResponse> Handle(ReportDeviceStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var serialNumber = request.DeviceId.ToUpperInvariant().Trim();

            // 1. Cihazı seri numarasıyla veritabanında arıyoruz
            var device = await dbContext.Devices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber && !d.IsDeleted, cancellationToken);

            if (device == null)
            {
                logger.LogWarning("⚠️ Bilinmeyen cihaz seri numarasıyla bağlantı durumu bildirme denemesi: {Serial}", serialNumber);

                // Yetkisiz erişim/deneme güvenlik olayı Kinesis'e fırlatılıyor 🚨
                await kinesisPublisher.PublishAsync(
                    partitionKey: serialNumber,
                    telemetryData: new
                    {
                        EventType = "SecurityAudit",
                        Action = "UnknownDeviceConnectionAttempt",
                        DeviceSerialNumber = serialNumber,
                        Message = $"Sistemde kayıtlı olmayan bir cihaz bağlantı kurmaya çalıştı: {serialNumber}",
                        Timestamp = DateTime.UtcNow
                    },
                    cancellationToken
                );

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

                await kinesisPublisher.PublishAsync(
                    partitionKey: device.SerialNumber,
                    telemetryData: new
                    {
                        EventType = "SecurityAudit",
                        Action = "InactiveDeviceConnectionAttempt",
                        DeviceSerialNumber = device.SerialNumber,
                        TenantId = device.TenantId,
                        Message = $"Pasif/askıya alınmış bir cihaz bağlantı kurmaya çalıştı: {device.Name} ({device.SerialNumber})",
                        Timestamp = DateTime.UtcNow
                    },
                    cancellationToken
                );

                return new ReportDeviceStatusResponse(
                    Message: $"Durum bildirimi reddedildi: Cihaz [{device.Name}] pasif durumda!",
                    IsSuccess: false,
                    ProcessedAt: DateTime.UtcNow
                );
            }

            // 3. Bağlantı/Durum Değişikliği Olayını Kinesis'e Aktar (Real-Time SIEM Pipeline) 🔌
            await kinesisPublisher.PublishAsync(
                partitionKey: device.SerialNumber,
                telemetryData: new
                {
                    EventType = "DeviceConnection",
                    DeviceSerialNumber = device.SerialNumber,
                    TenantId = device.TenantId,
                    IsOnline = request.IsOnline,
                    Message = $"Cihaz bağlantı durumu değişti: {device.Name} -> {(request.IsOnline ? "ONLINE" : "OFFLINE")}",
                    Timestamp = request.Timestamp
                },
                cancellationToken
            );

            logger.LogInformation("Cihaz bağlantı durumu Kinesis'e aktarıldı: {Serial} -> {Status}", 
                device.SerialNumber, request.IsOnline ? "ONLINE" : "OFFLINE");

            return new ReportDeviceStatusResponse(
                Message: $"Cihaz durumu başarıyla güncellendi ve Kinesis'e pompalandı: {device.Name} -> {(request.IsOnline ? "ONLINE" : "OFFLINE")}",
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
