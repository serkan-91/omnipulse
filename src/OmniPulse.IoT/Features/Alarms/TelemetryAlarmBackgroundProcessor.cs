using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniPulse.IoT.Features.Telemetry.IngestTelemetry;

namespace OmniPulse.IoT.Features.Alarms;

/// <summary>
/// Telemetri kuyruğunu (TelemetryQueue) dinleyen ve alarmları tamamen asenkron,
/// ayrı bir arka plan iş parçacığında çalıştırarak sistemi yormayan işleyici! ⚙️🔋
/// </summary>
public class TelemetryAlarmBackgroundProcessor(
    TelemetryQueue telemetryQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<TelemetryAlarmBackgroundProcessor> logger)
    : BackgroundService
{
    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("OmniPulse.IoTModule.TelemetryAlarmBackgroundProcessor");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Otonom Alarm Reaktör Arka Plan Servisi Başlatıldı! 🚀");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Kuyruktan bir sonraki telemetriyi bekle (Bloke etmeyen asenkron okuma)
                var telemetryEvent = await telemetryQueue.Reader.ReadAsync(stoppingToken);

                // OpenTelemetry Dağıtık İzlenebilirlik (Distributed Tracing) ve APM Bağlama 🔗 OTel
                var traceParent = telemetryEvent.TraceId;
                System.Diagnostics.Activity? activity = null;

                if (!string.IsNullOrEmpty(traceParent) && System.Diagnostics.ActivityContext.TryParse(traceParent, null, out var parentContext))
                {
                    activity = ActivitySource.StartActivity("CheckAlarmsBackground", System.Diagnostics.ActivityKind.Consumer, parentContext);
                }
                else
                {
                    activity = ActivitySource.StartActivity("CheckAlarmsBackground", System.Diagnostics.ActivityKind.Consumer);
                }

                using (activity)
                {
                    var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? telemetryEvent.TraceId ?? Guid.NewGuid().ToString();
                    using (logger.BeginScope(new System.Collections.Generic.Dictionary<string, object> { ["TraceId"] = traceId }))
                    {
                        // Bağımlılıkları (DbContext, EmailSender vb.) çözmek için Scoped alan yaratıyoruz
                        using var scope = scopeFactory.CreateScope();
                        var alarmService = scope.ServiceProvider.GetRequiredService<IAlarmService>();

                        // Alarm kontrolünü ve bildirim tetiklemelerini çalıştır
                        await alarmService.CheckAlarmsAsync(telemetryEvent, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Uygulama kapanırken normal durdurma akışı
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Arka planda telemetri alarm kontrolü yapılırken bir hata oluştu!");
            }
        }

        logger.LogInformation("Otonom Alarm Reaktör Arka Plan Servisi Durduruldu.");
    }
}
