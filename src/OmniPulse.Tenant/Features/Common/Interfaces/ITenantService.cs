using OmniPulse.Tenant.Domain.Entities;

namespace OmniPulse.Tenant.Features.Common.Interfaces;

public interface ITenantService
{
    // Şu anki istek atan aktif kiracının Identifier adını döner (Örn: "pandaberry")
    string? GetCurrentTenantIdentifier();

    // Şu anki kiracının özel PostgreSQL bağlantı cümlesini döner
    string? GetCurrentConnectionString();

    // Şu anki kiracının tüm domain bilgilerini nesne olarak döner
    OmniPulse.Tenant.Domain.Entities.Tenant? GetCurrentTenant();

    // Kiracı bağlamını el ile doldurmak (hydrate etmek) için kullanılan korumalı metod 🛡️
    void SetTenant(OmniPulse.Tenant.Domain.Entities.Tenant tenant);
}
