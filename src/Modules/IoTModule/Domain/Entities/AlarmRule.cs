using System;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Modules.IoTModule.Domain.Entities;

/// <summary>
/// IoT verilerinde aşılmaması gereken kırmızı çizgileri belirleyen alarm kuralları! 🚨🛑
/// </summary>
public class AlarmRule : BaseEntity, IAuditableEntity, ITenantEntity, ISoftDelete
{
    public Guid TenantId { get; set; }
    
    // Alarm kuralı ismi (Örn: "Yüksek Sıcaklık Uyarısı")
    public string Name { get; private set; } = null!;
    
    // Kuralın uygulanacağı cihaz (Null ise tüm kiracı cihazlarında geçerlidir)
    public Guid? DeviceId { get; private set; }
    public Device? Device { get; private set; }
    
    // Hangi metrik izlenecek (Örn: "Temperature", "Pressure")
    public string MetricKey { get; private set; } = null!;
    
    // Karşılaştırma değeri (Örn: 60.0)
    public double ThresholdValue { get; private set; }
    
    // Karşılaştırma operatörü (Örn: ">", "<", ">=", "<=")
    public string ComparisonOperator { get; private set; } = null!;
    
    // Kural aktif mi?
    public bool IsActive { get; private set; }

    // IAuditableEntity - Denetim/İzleme alanları
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDelete - Güvenli/Yumuşak silme alanları
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // EF Core için boş constructor
    private AlarmRule() { }

    public static AlarmRule Create(
        Guid tenantId,
        string name,
        Guid? deviceId,
        string metricKey,
        double thresholdValue,
        string comparisonOperator)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz şefim!", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kural adı boş bırakılamaz!", nameof(name));

        if (string.IsNullOrWhiteSpace(metricKey))
            throw new ArgumentException("Metrik anahtarı boş bırakılamaz!", nameof(metricKey));

        if (string.IsNullOrWhiteSpace(comparisonOperator))
            throw new ArgumentException("Karşılaştırma operatörü boş bırakılamaz!", nameof(comparisonOperator));

        return new AlarmRule
        {
            TenantId = tenantId,
            Name = name.Trim(),
            DeviceId = deviceId,
            MetricKey = metricKey.Trim(),
            ThresholdValue = thresholdValue,
            ComparisonOperator = comparisonOperator.Trim(),
            IsActive = true
        };
    }

    public void UpdateDetails(string name, string metricKey, double thresholdValue, string comparisonOperator, bool isActive)
    {
        Name = name.Trim();
        MetricKey = metricKey.Trim();
        ThresholdValue = thresholdValue;
        ComparisonOperator = comparisonOperator.Trim();
        IsActive = isActive;
    }
}
