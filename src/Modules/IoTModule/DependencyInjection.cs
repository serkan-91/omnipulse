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

        // Kuyruk ve Arka Plan Alarm İşleme Servisleri 🚨🔋
        services.AddSingleton<Features.Telemetry.IngestTelemetry.TelemetryQueue>();
        services.AddScoped<Features.Alarms.IAlarmService, Features.Alarms.AlarmService>();
        services.AddHostedService<Features.Alarms.TelemetryAlarmBackgroundProcessor>();

        // MediatR IPipelineBehavior Önbellekleme Kaydı 🌳⚡
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.DeviceCategories.DeviceCategoryCacheBehavior<,>));

        return services;
    }
}
