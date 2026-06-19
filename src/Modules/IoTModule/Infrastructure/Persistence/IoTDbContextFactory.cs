using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

public class IoTDbContextFactory : IDesignTimeDbContextFactory<IoTDbContext>
{
    public IoTDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IoTDbContext>();
        
        // Bazzite Linux üzerindeki yerel veritabanı bağlantısı
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=omnipulse_shared;Username=omnipulse_admin;Password=SuperSecurePassword123!;");

        return new IoTDbContext(optionsBuilder.Options);
    }
}
