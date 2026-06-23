using Microsoft.EntityFrameworkCore;
using OmniPulse.BuildingBlocks.Interfaces;
using OmniPulse.BuildingBlocks.Extensions;
using OmniPulse.Workflow.Domain.Entities;
using OmniPulse.Workflow.Infrastructure.Persistence.Configurations;

namespace OmniPulse.Workflow.Infrastructure.Persistence;

public class WorkflowDbContext(
    DbContextOptions<WorkflowDbContext> options,
    IUserTenantContext userTenantContext) 
    : DbContext(options)
{
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<AssignmentPolicy> AssignmentPolicies => Set<AssignmentPolicy>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=omnipulse_shared;Username=omnipulse_admin;Password=SuperSecurePassword123!;");
        }
        base.OnConfiguring(optionsBuilder);
    }

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

        modelBuilder.ApplyConfiguration(new WorkflowDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentPolicyConfiguration());

        // Modeldeki tüm entity'ler için dinamik filtreleme ayarları (Soft delete ve Tenant izolasyonu)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    ConvertSoftDeleteFilterExpression(entityType.ClrType));
            }

            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    ConvertTenantFilterExpression(entityType.ClrType, userTenantContext));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression ConvertSoftDeleteFilterExpression(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var notExpression = System.Linq.Expressions.Expression.Not(property);
        return System.Linq.Expressions.Expression.Lambda(notExpression, parameter);
    }

    private static System.Linq.Expressions.LambdaExpression ConvertTenantFilterExpression(
        Type type, 
        IUserTenantContext context)
    {
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
