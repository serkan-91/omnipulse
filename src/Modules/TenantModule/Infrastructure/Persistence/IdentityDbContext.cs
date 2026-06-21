using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence.Configurations;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.BuildingBlocks.Extensions;

namespace OmniPulse.Modules.TenantModule.Infrastructure.Persistence;

public class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options, 
    ITenantService tenantService,
    IUserTenantContext userTenantContext)
    : DbContext(options)
{ 
    // Veritabanındaki Tenants tablomuz
    public DbSet<Tenant> Tenants => Set<Tenant>();
    
    // Veritabanındaki Güvenlik/Auth Günlükleri
    public DbSet<SecurityLog> SecurityLogs => Set<SecurityLog>();

    // Veritabanındaki Müşteriler tablomuz
    public DbSet<Customer> Customers => Set<Customer>();

    // Veritabanındaki Kullanıcılar tablomuz
    public DbSet<User> Users => Set<User>();

    // Kullanıcı-Kiracı çoka-çok ilişki tablomuz
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    // Veritabanındaki Refresh Token tablomuz
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public override int SaveChanges()
    {
        DbContextExtensions.ApplyAuditingAndSoftDelete(this, userTenantContext);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DbContextExtensions.ApplyAuditingAndSoftDelete(this, userTenantContext);
        return base.SaveChangesAsync(cancellationToken);
    }

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
        modelBuilder.ApplyConfiguration(new SecurityLogConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TenantUserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

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
