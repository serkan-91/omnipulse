using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule;

public static class DependencyInjection
{
    public static IServiceCollection AddIoTModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IoTConnection");

        // IoTDbContext'i de sisteme ekliyoruz ki göçler (migrations) yapılabilsin
        services.AddDbContext<IoTDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("OmniPulse.Identity.API"));
        });

        // MediatR artık tam çalışacak!
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
