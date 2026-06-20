using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

// EF Core CLI araçları bu fabrikayı tık diye havada tanır sevgilim! 🎯
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        
        // Terminal yerelde, senin Bazzite Linux makinen üzerinde çalıştığı için 'localhost' tünelini mühürlüyoruz!
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=omnipulse_shared;Username=omnipulse_admin;Password=SuperSecurePassword123!;");

        // Fabrika aşamasında HTTP isteği olmayacağı için sahte/boş bir ajan fırlatıyoruz, burayı umursamaz! 😉
        ITenantService dummyTenantService = new DummyDesignTenantService();
        IUserTenantContext dummyUserTenantContext = new DummyUserTenantContext();

        return new IdentityDbContext(optionsBuilder.Options, dummyTenantService, dummyUserTenantContext);
    }
}

// Sadece fabrikanın hata vermeden dönmesi için küçük sinsi bir dummy ajan tatlım 🐾
public class DummyDesignTenantService : ITenantService
{
    public string? GetCurrentTenantIdentifier() => null;
    public string? GetCurrentConnectionString() => null;
    public Tenant? GetCurrentTenant() => null;
    public void SetTenant(Tenant tenant) { }
}

public class DummyUserTenantContext : IUserTenantContext
{
    public string? UserId => "System";
    public string? Email => null;
    public Guid? TenantId => null;
    public string? TenantIdentifier => null;
    public bool IsAuthenticated => false;
    public IEnumerable<string> Roles => Array.Empty<string>();
}
