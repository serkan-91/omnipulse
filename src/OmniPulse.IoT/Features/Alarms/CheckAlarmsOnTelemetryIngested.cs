using MediatR;
using OmniPulse.IoT.Features.Telemetry.IngestTelemetry;

namespace OmniPulse.IoT.Features.Alarms;

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
