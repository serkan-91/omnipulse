using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Hubs;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;
using OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

namespace OmniPulse.Modules.IoTModule.Features.Alarms;

public interface IAlarmService
{
    Task CheckAlarmsAsync(TelemetryIngestedEvent notification, CancellationToken cancellationToken);
}

public class AlarmService(
    IoTDbContext dbContext,
    IHubContext<AlarmHub> hubContext,
    IEmailSender emailSender)
    : IAlarmService
{
    public async Task CheckAlarmsAsync(TelemetryIngestedEvent notification, CancellationToken cancellationToken)
    {
        // 1. O kiracıya ait aktif ve silinmemiş alarm kurallarını sorgulayalım
        var rules = await dbContext.AlarmRules
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == notification.TenantId && r.IsActive && !r.IsDeleted &&
                        (r.DeviceId == notification.DeviceId || r.DeviceId == null))
            .ToListAsync(cancellationToken);

        if (rules.Count == 0) return;

        foreach (var rule in rules)
        {
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
                continue;
            }

            bool isBreached = EvaluateThreshold(valueToCheck, rule.ThresholdValue, rule.ComparisonOperator);

            var activeAlarm = await dbContext.AlarmEvents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.TenantId == notification.TenantId &&
                                          e.DeviceId == notification.DeviceId &&
                                          e.AlarmRuleId == rule.Id &&
                                          !e.IsResolved, cancellationToken);

            if (isBreached)
            {
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

                    // SignalR Broadcast
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

                    // E-posta gönder
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
                if (activeAlarm != null)
                {
                    activeAlarm.Resolve(DateTime.UtcNow);
                    dbContext.AlarmEvents.Update(activeAlarm);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // SignalR Broadcast
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

                    // E-posta gönder
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
