using OmniPulse.Modules.TenantModule.Domain.Entities;

namespace OmniPulse.Modules.TenantModule.Features.Common.Interfaces;

public interface ITenantService
{
    // Şu anki istek atan aktif kiracının Identifier adını döner (Örn: "pandaberry")
    string? GetCurrentTenantIdentifier();

    // Şu anki kiracının özel PostgreSQL bağlantı cümlesini döner
    string? GetCurrentConnectionString();

    // Şu anki kiracının tüm domain bilgilerini nesne olarak döner
    Tenant? GetCurrentTenant();

    // Kiracı bağlamını el ile doldurmak (hydrate etmek) için kullanılan korumalı metod 🛡️
    void SetTenant(Tenant tenant);
}
