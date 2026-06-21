using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniPulse.BuildingBlocks.Configuration;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule;

public static class DependencyInjection
{
    public static IServiceCollection AddIoTModule(this IServiceCollection services, IConfiguration configuration)
    {
        // DatabaseOptions kiracı modülünde zaten kaydedilmiştir;
        // IoTModule sadece IoTConnection'ı tüketir — çift kayıt olmaz.
        services.AddDbContext<IoTDbContext>((sp, options) =>
        {
            var dbOpts = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(dbOpts.IoTConnection,
                b => b.MigrationsAssembly("OmniPulse.Identity.API"));
        });

        // MediatR artık tam çalışacak!
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Kuyruk ve Arka Plan Alarm İşleme Servisleri 🚨🔋
        services.AddSingleton<Features.Telemetry.IngestTelemetry.TelemetryQueue>();
        services.AddScoped<Features.Alarms.IAlarmService, Features.Alarms.AlarmService>();
        services.AddScoped<OmniPulse.BuildingBlocks.Interfaces.IIotAssetService, Infrastructure.Services.IotAssetService>();
        services.AddHostedService<Features.Alarms.TelemetryAlarmBackgroundProcessor>();
        services.AddHostedService<Infrastructure.Services.OutboxEventProcessor>();
        services.AddHostedService<Infrastructure.Services.PostgresPartitionManager>();

        // AWS Kinesis IAmazonKinesis İstemcisi ve Yayıncı Kaydı (LocalStack veya Canlı AWS)
        var kinesisSection = configuration.GetSection("AWS:Kinesis");
        var useLocalStack = kinesisSection.GetValue<bool>("UseLocalStack");
        var regionName = kinesisSection.GetValue<string>("Region") ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(regionName);

        services.AddSingleton<Amazon.Kinesis.IAmazonKinesis>(sp =>
        {
            try
            {
                if (useLocalStack)
                {
                    return new Amazon.Kinesis.AmazonKinesisClient(new Amazon.Kinesis.AmazonKinesisConfig
                    {
                        ServiceURL = "http://localhost:4566",
                        AuthenticationRegion = regionEndpoint.SystemName
                    });
                }
                return new Amazon.Kinesis.AmazonKinesisClient(regionEndpoint);
            }
            catch
            {
                return null!;
            }
        });

        services.AddSingleton<Infrastructure.Streaming.IKinesisTelemetryPublisher, Infrastructure.Streaming.KinesisTelemetryPublisher>();

        // MediatR IPipelineBehavior Önbellekleme Kaydı 🌳⚡
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.DeviceCategories.DeviceCategoryCacheBehavior<,>));

        return services;
    }
}
