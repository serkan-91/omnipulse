using System;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Verinin tablodan fiziksel olarak silinmesini engelleyip, arka planda pasifleştiren kalkan! 🛡️
/// </summary>
public interface ISoftDelete
{
    // Verinin silinip silinmediği bayrağı
    bool IsDeleted { get; set; }

    // Verinin silindiği UTC zaman damgası
    DateTime? DeletedAtUtc { get; set; }

    // Veriyi silen kullanıcının kimliği
    string? DeletedBy { get; set; }
}
