using System;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.IoT.Domain.Entities;

/// <summary>
/// Bir alarm kuralı tetiklendiğinde oluşan vaka kaydı! 🚨💥
/// Hangi cihazda, ne zaman, hangi değerle alarm oluştuğunu saklar ve çözüm durumunu takip eder.
/// </summary>
public class AlarmEvent : BaseEntity, IAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    // Alarmın oluştuğu cihaz
    public Guid DeviceId { get; private set; }
    public Device Device { get; private set; } = null!;
    
    // Tetiklenen kural
    public Guid AlarmRuleId { get; private set; }
    public AlarmRule AlarmRule { get; private set; } = null!;
    
    // Kuralı tetikleyen anlık telemetri değeri
    public double TriggeredValue { get; private set; }
    
    // Aşılması yasak olan kural eşik değeri
    public double ThresholdValue { get; private set; }
    
    // Alarm mesajı (Örn: "Sensör sıcaklığı 62°C ile 60°C eşiğini aştı!")
    public string Message { get; private set; } = null!;
    
    // Alarmın tetiklenme anı
    public DateTime TriggeredAtUtc { get; private set; }
    
    // Alarm çözüldü mü (durum normale döndü mü)?
    public bool IsResolved { get; private set; }
    
    // Alarmın normal duruma dönme zamanı
    public DateTime? ResolvedAtUtc { get; private set; }

    // IAuditableEntity - Denetim/İzleme alanları
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // EF Core için boş constructor
    private AlarmEvent() { }

    public static AlarmEvent Create(
        Guid tenantId,
        Guid deviceId,
        Guid alarmRuleId,
        double triggeredValue,
        double thresholdValue,
        string message,
        DateTime triggeredAtUtc)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz şefim!", nameof(tenantId));

        if (deviceId == Guid.Empty)
            throw new ArgumentException("Cihaz ID'si boş bırakılamaz!", nameof(deviceId));

        if (alarmRuleId == Guid.Empty)
            throw new ArgumentException("Kural ID'si boş bırakılamaz!", nameof(alarmRuleId));

        return new AlarmEvent
        {
            TenantId = tenantId,
            DeviceId = deviceId,
            AlarmRuleId = alarmRuleId,
            TriggeredValue = triggeredValue,
            ThresholdValue = thresholdValue,
            Message = message.Trim(),
            TriggeredAtUtc = triggeredAtUtc,
            IsResolved = false
        };
    }

    public void Resolve(DateTime resolvedAtUtc)
    {
        IsResolved = true;
        ResolvedAtUtc = resolvedAtUtc;
    }
}
