using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using OmniPulse.BuildingBlocks.Configuration;
using OmniPulse.Modules.TenantModule.Domain.Entities;
using OmniPulse.Modules.TenantModule.Infrastructure.Persistence;
using OmniPulse.Modules.TenantModule.Infrastructure.MultiTenancy;
using OmniPulse.Modules.TenantModule.Features.Common.Interfaces;

namespace OmniPulse.Modules.TenantModule;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantModule(this IServiceCollection services, IConfiguration configuration)
    {
        // DatabaseOptions'ı configuration'dan bağla ve doğrula
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantService, TenantService>();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var dbOpts = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            // Göçleri API projesinde toplamak için MigrationAssembly'i buraya mühürlüyoruz!
            options.UseNpgsql(dbOpts.DefaultConnection,
                b => b.MigrationsAssembly("OmniPulse.Identity.API"));
        });

        // Şifreleri güvenli hash'leme kalkanı! 🔐
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
