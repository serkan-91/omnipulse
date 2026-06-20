using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Hubs;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;
using OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

namespace OmniPulse.Modules.IoTModule.Features.Alarms;

/// <summary>
/// Yeni telemetri verisi alındığında tetiklenen, kuralları değerlendiren,
/// SignalR ve E-posta kanallarını otonom olarak besleyen alarm reaktörü! 🚨🤖
/// </summary>
public class CheckAlarmsOnTelemetryIngested(
    IoTDbContext dbContext,
    IHubContext<AlarmHub> hubContext,
    IEmailSender emailSender)
    : INotificationHandler<TelemetryIngestedEvent>
{
    public async Task Handle(TelemetryIngestedEvent notification, CancellationToken cancellationToken)
    {
        // 1. O kiracıya ait aktif ve silinmemiş alarm kurallarını sorgulayalım.
        // Arka plan işlemleri kiracı kısıtlamalarını bypass edebileceğinden IgnoreQueryFilters() uyguluyoruz.
        var rules = await dbContext.AlarmRules
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == notification.TenantId && r.IsActive && !r.IsDeleted &&
                        (r.DeviceId == notification.DeviceId || r.DeviceId == null))
            .ToListAsync(cancellationToken);

        if (rules.Count == 0) return;

        foreach (var rule in rules)
        {
            // Ölçüm türünü doğrula
            double valueToCheck = 0;
            if (rule.MetricKey.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            {
                valueToCheck = notification.Temperature;
            }
            else if (rule.MetricKey.Equals("Pressure", StringComparison.OrdinalIgnoreCase))
            {
                valueToCheck = notification.Pressure;
            }
            else
            {
                continue; // Desteklenmeyen metrik
            }

            // Kural ihlal durumunu değerlendir
            bool isBreached = EvaluateThreshold(valueToCheck, rule.ThresholdValue, rule.ComparisonOperator);

            // Cihazın ve kuralın o andaki aktif (unresolved/çözülmemiş) alarm kaydını getir
            var activeAlarm = await dbContext.AlarmEvents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.TenantId == notification.TenantId &&
                                          e.DeviceId == notification.DeviceId &&
                                          e.AlarmRuleId == rule.Id &&
                                          !e.IsResolved, cancellationToken);

            if (isBreached)
            {
                // Değer eşiği aştı ve önceden oluşmuş açık bir alarm kaydı yoksa alarm üret! (Spam önleme)
                if (activeAlarm == null)
                {
                    var message = $"Kritik Eşik Aşımı! [{notification.DeviceName}] cihazında ölçülen {rule.MetricKey}: {valueToCheck}, belirlenen {rule.ThresholdValue} eşiğini aştı! 🚨";
                    
                    var alarmEvent = AlarmEvent.Create(
                        notification.TenantId,
                        notification.DeviceId,
                        rule.Id,
                        valueToCheck,
                        rule.ThresholdValue,
                        message,
                        DateTime.UtcNow
                    );

                    dbContext.AlarmEvents.Add(alarmEvent);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // A. SignalR Hub ile Kiracının Canlı Panelindeki Mehmet Usta'ya bildirim fırlat! 📡
                    await hubContext.Clients.Group(notification.TenantId.ToString())
                        .SendAsync("ReceiveAlarm", new
                        {
                            AlarmId = alarmEvent.Id,
                            DeviceName = notification.DeviceName,
                            MetricKey = rule.MetricKey,
                            Value = valueToCheck,
                            Threshold = rule.ThresholdValue,
                            Message = message,
                            TriggeredAt = alarmEvent.TriggeredAtUtc
                        }, cancellationToken);

                    // B. Mehmet Usta'ya Acil E-posta Gönder! 📧
                    await emailSender.SendEmailAsync(
                        "mehmet.usta@pandaberry.com",
                        $"ACİL: {notification.DeviceName} - Kritik Sensör Alarmı!",
                        $"""
                        Sayın Mehmet Usta,
                        
                        Sistemimiz üzerinden kritik bir sensör ihlali algılandı:
                        
                        Cihaz: {notification.DeviceName}
                        Metrik: {rule.MetricKey}
                        Ölçülen Değer: {valueToCheck}
                        Kural Eşiği: {rule.ThresholdValue}
                        Zaman Damgası (UTC): {notification.Timestamp}
                        
                        Lütfen ilgili tırı ve sensörü kontrol ediniz.
                        
                        OmniPulse Otonom Bildirim Servisi 🤖
                        """
                    );
                }
            }
            else
            {
                // Değer normale döndü ve açık bir alarm varsa alarmı çözüldü olarak işaretle!
                if (activeAlarm != null)
                {
                    activeAlarm.Resolve(DateTime.UtcNow);
                    dbContext.AlarmEvents.Update(activeAlarm);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // A. SignalR Hub üzerinden normale dönüş bilgisini gönder
                    await hubContext.Clients.Group(notification.TenantId.ToString())
                        .SendAsync("AlarmResolved", new
                        {
                            AlarmId = activeAlarm.Id,
                            DeviceName = notification.DeviceName,
                            MetricKey = rule.MetricKey,
                            Value = valueToCheck,
                            Message = $"Durum Normale Döndü: {notification.DeviceName} - Sıcaklık/Basınç normale döndü. ✅",
                            ResolvedAt = activeAlarm.ResolvedAtUtc
                        }, cancellationToken);

                    // B. Mehmet Usta'ya durum düzeldi e-postası gönder
                    await emailSender.SendEmailAsync(
                        "mehmet.usta@pandaberry.com",
                        $"BİLGİ: {notification.DeviceName} - Alarm Çözüldü",
                        $"""
                        Sayın Mehmet Usta,
                        
                        [{notification.DeviceName}] cihazındaki alarm durumu son bulmuş ve ölçülen değer normale dönmüştür:
                        
                        Ölçülen Değer: {valueToCheck}
                        Zaman (UTC): {DateTime.UtcNow}
                        
                        İyi çalışmalar dileriz.
                        
                        OmniPulse Otonom Bildirim Servisi 🤖
                        """
                    );
                }
            }
        }
    }

    private static bool EvaluateThreshold(double value, double threshold, string op)
    {
        return op switch
        {
            ">" => value > threshold,
            "<" => value < threshold,
            ">=" => value >= threshold,
            "<=" => value <= threshold,
            "==" => Math.Abs(value - threshold) < 0.0001,
            _ => false
        };
    }
}
