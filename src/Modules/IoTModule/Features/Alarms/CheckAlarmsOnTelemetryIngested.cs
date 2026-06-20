using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniPulse.Modules.IoTModule.Features.Telemetry.IngestTelemetry;

namespace OmniPulse.Modules.IoTModule.Features.Alarms;

/// <summary>
/// Telemetri verisi alındığında MediatR tarafından çağrılan ilk reaktör.
/// Görevi sadece veriyi arka planda asenkron işlenecek kuyruğa yazıp hemen HTTP isteğini özgür bırakmaktır! ⚡🚀
/// </summary>
public class CheckAlarmsOnTelemetryIngested(TelemetryQueue queue)
    : INotificationHandler<TelemetryIngestedEvent>
{
    public Task Handle(TelemetryIngestedEvent notification, CancellationToken cancellationToken)
    {
        queue.Writer.TryWrite(notification);
        return Task.CompletedTask;
    }
}
