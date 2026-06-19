using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence;
using OmniPulse.Modules.TenantModule.Infrastructure.MultiTenancy;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;

namespace OmniPulse.Modules.TenantModule;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantService, TenantService>();

        var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            // Göçleri API projesinde toplamak için MigrationAssembly'i buraya mühürlüyoruz!
            options.UseNpgsql(defaultConnectionString, b => b.MigrationsAssembly("OmniPulse.Identity.API"));
        });

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
