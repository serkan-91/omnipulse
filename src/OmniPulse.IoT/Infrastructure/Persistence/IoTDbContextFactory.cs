using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OmniPulse.BuildingBlocks.Interfaces;

namespace OmniPulse.IoT.Infrastructure.Persistence;

public class IoTDbContextFactory : IDesignTimeDbContextFactory<IoTDbContext>
{
    public IoTDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IoTDbContext>();
        
        // Bazzite Linux üzerindeki yerel veritabanı bağlantısı
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=omnipulse_shared;Username=omnipulse_admin;Password=SuperSecurePassword123!;");

        IUserTenantContext dummyUserTenantContext = new DummyUserTenantContext();

        return new IoTDbContext(optionsBuilder.Options, dummyUserTenantContext);
    }
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
