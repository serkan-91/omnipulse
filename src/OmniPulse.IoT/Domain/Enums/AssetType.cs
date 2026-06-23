namespace OmniPulse.IoT.Domain.Enums;

/// <summary>
/// Varlık türünü sektör-bağımsız olarak tanımlar.
/// Aynı sistem hem tır filosunu hem fabrika bandını hem de akıllı şehir altyapısını yönetebilir.
/// </summary>
public enum AssetType
{
    /// <summary>Tır, kamyon, soğutmalı araç gibi mobil taşıtlar.</summary>
    Vehicle = 1,

    /// <summary>Fabrika konveyör bantları, montaj hatları.</summary>
    ConveyorBelt = 2,

    /// <summary>Soğutucu dolap, inkübatör, dondurma kabini, soğuk oda.</summary>
    FreezerIncubator = 3,

    /// <summary>Su vanası, elektrik trafosu, gaz hattı gibi altyapı bileşenleri.</summary>
    ValveInfrastructure = 4,

    /// <summary>Oda, kat, bölge veya alan bazlı gruplamalar.</summary>
    RoomArea = 5,

    /// <summary>Isı, basınç, titreşim veya su seviye sensörleri gibi donanım sensörlerini temsil eden varlıklar.</summary>
    Sensor = 6,

    /// <summary>Yukarıdaki kategorilere girmeyen özel tanımlı varlıklar.</summary>
    Custom = 99
}
