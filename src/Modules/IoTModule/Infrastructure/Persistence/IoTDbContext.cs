using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Configurations;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

public class IoTDbContext(DbContextOptions<IoTDbContext> options) : DbContext(options)
{
    public DbSet<Telemetry> Telemetries => Set<Telemetry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TelemetryConfiguration());
    }
}
