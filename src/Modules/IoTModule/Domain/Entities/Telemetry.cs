using System;
using OmniPulse.BuildingBlocks.Common;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Modules.IoTModule.Domain.Entities;

/// <summary>
/// Sahadaki IoT cihazlarından akan ham telemetri verilerinin kalbi ve kalesi! 📊🌡️
/// Her sensörün sıcaklık, basınç ve zaman damgası verilerini burada mühürlüyoruz.
/// </summary>
public class Telemetry : BaseEntity, IAuditableEntity, ITenantEntity
{
    // Kiracının kimliği (Veri izolasyonu için!)
    public Guid TenantId { get; set; }

    // İlişkili sensör donanımının veri tabanı ID'si
    public Guid DeviceId { get; private set; }
    public Device Device { get; private set; } = null!;

    // Cihazın ölçtüğü anlık sıcaklık değeri
    public double Temperature { get; private set; }

    // Cihazın ölçtüğü anlık basınç değeri
    public double Pressure { get; private set; }

    // Cihazın bu veriyi tam olarak sahada ürettiği zaman damgası (ISO 8601 / UTC)
    public DateTime Timestamp { get; private set; }

    // IAuditableEntity - Denetim/İzleme alanları
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // Entity Framework Core için boş constructor
    private Telemetry() { }

    // DDD Standartlarına uygun, korunaklı ve tertemiz nesne doğurma metodu! ✨🚀
    public static Telemetry Create(Guid tenantId, Guid deviceId, double temperature, double pressure, DateTime timestamp)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Kiracı ID'si boş bırakılamaz şefim!", nameof(tenantId));

        if (deviceId == Guid.Empty)
            throw new ArgumentException("IoT Cihaz kimliği (DeviceId) boş bırakılamaz!", nameof(deviceId));

        if (timestamp > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Gelecekten gelen zaman damgası kabul edilemez, sinsi bir şeyler dönüyor! 🌌", nameof(timestamp));

        return new Telemetry
        {
            TenantId = tenantId,
            DeviceId = deviceId,
            Temperature = temperature,
            Pressure = pressure,
            Timestamp = timestamp
        };
    }
}