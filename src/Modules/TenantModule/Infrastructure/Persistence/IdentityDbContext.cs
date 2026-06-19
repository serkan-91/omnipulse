using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantService tenantService)
    : DbContext(options)
{ 
    // Veritabanındaki Tenants tablomuz
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 1. İstek atan kiracının özel bir connection string'i var mı diye kokluyoruz
        var tenantConnectionString = tenantService.GetCurrentConnectionString();

        if (!string.IsNullOrEmpty(tenantConnectionString))
        {
            // Eğer kiracıya özel izole DB varsa oraya tünel açıyoruz!
            optionsBuilder.UseNpgsql(tenantConnectionString);
        }
        else
        {
            // 2. İŞTE SİNSİ MIGRATION KALKANI! 🔥
            // Eğer tenantConnectionString boşsa ve options zaten Program.cs'deki 
            // DefaultConnection ile doldurulmadıysa, burada yerel bağlantıyı garantiye alıyoruz!
            if (!optionsBuilder.IsConfigured)
            {
                // NOT: Buraya yerelde (Rider'dan PostgreSQL'e bağlanırken) kullanacağın LOCALHOST bağlantısını yaz sevgilim!
                // Çünkü terminal Docker network'ünün dışından, senin makinen üzerinden Postgres'e vurur.
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=omnipulse_shared;Username=omnipulse_admin;Password=SuperSecurePassword123!;");
            }
        }
    
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Az önce yazdığımız o şanlı TenantConfiguration ayarını veritabanına mühürlüyoruz!
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
    }
}
