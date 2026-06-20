using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Domain.Entities;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Configurations;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.BuildingBlocks.Extensions;

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

public class IoTDbContext(
    DbContextOptions<IoTDbContext> options,
    IUserTenantContext userTenantContext) 
    : DbContext(options)
{
    public DbSet<Telemetry> Telemetries => Set<Telemetry>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<DeviceCategory> DeviceCategories => Set<DeviceCategory>();
    public DbSet<Device> Devices => Set<Device>();

    public override int SaveChanges()
    {
        this.ApplyAuditingAndSoftDelete(userTenantContext);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyAuditingAndSoftDelete(userTenantContext);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TelemetryConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceConfiguration());

        // Modeldeki tüm entity'ler için dinamik filtreleme ayarları
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 1. Soft Delete Filtrelemesi
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    ConvertSoftDeleteFilterExpression(entityType.ClrType));
            }

            // 2. Kiracı (Tenant) İzolasyon Filtrelemesi
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    ConvertTenantFilterExpression(entityType.ClrType, userTenantContext));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression ConvertSoftDeleteFilterExpression(Type type)
    {
        // e => !e.IsDeleted
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var notExpression = System.Linq.Expressions.Expression.Not(property);
        return System.Linq.Expressions.Expression.Lambda(notExpression, parameter);
    }

    private static System.Linq.Expressions.LambdaExpression ConvertTenantFilterExpression(
        Type type, 
        IUserTenantContext context)
    {
        // e => !context.TenantId.HasValue || e.TenantId == context.TenantId.Value
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        
        var tenantIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        
        var contextTenantIdExpression = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Constant(context), 
            nameof(IUserTenantContext.TenantId));

        var hasValueExpression = System.Linq.Expressions.Expression.Property(
            contextTenantIdExpression,
            nameof(Nullable<Guid>.HasValue));

        var notHasValueExpression = System.Linq.Expressions.Expression.Not(hasValueExpression);

        var valueExpression = System.Linq.Expressions.Expression.Property(
            contextTenantIdExpression,
            nameof(Nullable<Guid>.Value));

        var equalsExpression = System.Linq.Expressions.Expression.Equal(tenantIdProperty, valueExpression);

        var body = System.Linq.Expressions.Expression.OrElse(notHasValueExpression, equalsExpression);

        return System.Linq.Expressions.Expression.Lambda(body, parameter);
    }
}
