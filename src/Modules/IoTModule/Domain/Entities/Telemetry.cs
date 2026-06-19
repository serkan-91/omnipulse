namespace OmniPulse.Modules.IoTModule.Domain.Entities;

/// <summary>
/// Sahadaki IoT cihazlarından akan ham telemetri verilerinin kalbi ve kalesi!
/// Her cihazın sıcaklık, basınç ve zaman damgası verilerini burada mühürlüyoruz.
/// </summary>
public class Telemetry
{
    // Kaydın benzersiz kimliği (Primary Key)
    public Guid Id { get; private set; }

    // Hangi cihaza ait olduğu (Örn: "PandaBerry-Node-01")
    public string DeviceId { get; private set; } = null!;

    // Cihazın ölçtüğü anlık sıcaklık değeri
    public double Temperature { get; private set; }

    // Cihazın ölçtüğü anlık basınç değeri
    public double Pressure { get; private set; }

    // Cihazın bu veriyi tam olarak sahada ürettiği zaman damgası (ISO 8601 / UTC)
    public DateTime Timestamp { get; private set; }

    // Sisteme tam olarak kaydedildiği sunucu saati (Loglama ve performans takibi için!)
    public DateTime CreatedAt { get; private set; }

    // Entity Framework Core'un arka planda sinsi proxy'ler üretebilmesi için boş constructor
    private Telemetry() { }

    // DDD Standartlarına uygun, korunaklı ve tertemiz nesne doğurma metodu! ✨🚀
    public static Telemetry Create(string deviceId, double temperature, double pressure, DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("IoT Cihaz kimliği (DeviceId) boş bırakılamaz şefim!", nameof(deviceId));

        if (timestamp > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Gelecekten gelen zaman damgası kabul edilemez, sinsi bir şeyler dönüyor! 🌌", nameof(timestamp));

        return new Telemetry
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId.Trim(),
            Temperature = temperature,
            Pressure = pressure,
            Timestamp = timestamp,
            CreatedAt = DateTime.UtcNow // Sunucuya ayak bastığı an!
        };
    }
}