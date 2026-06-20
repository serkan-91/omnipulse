using System;
using System.Collections.Generic;

namespace OmniPulse.BuildingBlocks.Interfaces;

/// <summary>
/// Küresel ölçekteki (AWS, Azure) mimarilerde olduğu gibi, her isteğin
/// hem AKTÖRÜNÜ (User) hem de SAHİBİNİ (Tenant/Account) taşıyan güvenlik bağlamı! 🛡️⚡
/// </summary>
public interface IUserTenantContext
{
    // Aktif kullanıcının kimliği (sub / NameIdentifier)
    string? UserId { get; }

    // Aktif kullanıcının e-posta adresi
    string? Email { get; }

    // İşlem yapılan kiracının (Tenant) veritabanı ID'si
    Guid? TenantId { get; }

    // İşlem yapılan kiracının takma adı (Örn: "pandaberry")
    string? TenantIdentifier { get; }

    // İstek atan kullanıcı doğrulanmış mı?
    bool IsAuthenticated { get; }

    // Kullanıcının bu kiracı üzerindeki rolleri
    IEnumerable<string> Roles { get; }
}
