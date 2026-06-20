using System;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Veri tabanında ekleme ve güncelleme işlemlerini kimin, ne zaman yaptığını takip eden şanlı arayüz! 🚀
/// </summary>
public interface IAuditableEntity
{
    // Kaydın ilk oluşturulduğu UTC zaman damgası
    DateTime CreatedAtUtc { get; set; }

    // Kaydı oluşturan kullanıcının kimliği (kullanıcı adı, email veya Guid)
    string? CreatedBy { get; set; }

    // Kaydın son güncellendiği UTC zaman damgası
    DateTime? LastModifiedAtUtc { get; set; }

    // Kaydı en son güncelleyen kullanıcının kimliği
    string? LastModifiedBy { get; set; }
}
